# deploy.yml 自动部署流水线 · 新手学习手册

> 本文档配套 `.github/workflows/` 下的三个文件，帮你理解：
> **代码推上去之后，GitHub 是怎么自动判断"改了前端还是后端"，并只发布对应那部分的。**

---

## 1. 一句话概括

`deploy.yml` 是一个 **主调度文件**：它自己不构建、不发布，只负责
**"侦测改动 → 决定要不要跑前端 / 后端 → 调用对应的子工作流去真正发布"**。

---

## 2. 文件结构（三个文件各司其职）

```
.github/workflows/
├── deploy.yml            ← 主调度器（本文重点）：监听+判断+路由
├── deploy-frontend.yml   ← 子流程1：前端构建并发布到 IIS（on: workflow_call）
└── deploy-backend.yml    ← 子流程2：后端发布（on: workflow_call，目前是骨架）
```

- **主文件** `deploy.yml`：`on` 里写 `push` / `workflow_dispatch`，所以只有它会被"自动触发"。
- **子文件** 两个：`on` 写的是 `workflow_call`，意思是"我不会被自动触发，我是被别人 `uses: ` 调用的"。

这种"主文件调用子文件"的玩法，在 GitHub Actions 里叫 **可复用工作流（reusable workflow）**。
好处：前端和后端的发布逻辑各写一份、互不干扰，主文件只做调度。

---

## 3. 核心原理：怎样实现"监听前端/后端改动，触发对应子流程"

这是最关键的问题，拆成 3 步理解：

### 第 1 步：监听事件（`on`）
```yaml
on:
  push:
    branches: [ main ]     # 推送到 main → 自动触发
  workflow_dispatch:        # 网页手动点 Run workflow → 手动触发
```
只要有人往 `main` 推代码，整条流水线就启动了。

### 第 2 步：判断"这次改了哪里"（`detect-changes` 任务）
用 Action **`dorny/paths-filter`** 对比本次提交改了哪些文件，再按规则匹配目录：

```yaml
filters:
  backend:
    - 'PMD_Backend/**'     # 后端目录下的任何文件改动
  frontend:
    - 'PMD_React/**'       # 前端目录下的任何文件改动
```

对比结果会变成两个布尔值（true/false）：
- 改了 `PMD_Backend` 里的文件 → `backend = true`
- 改了 `PMD_React` 里的文件 → `frontend = true`
- 两个都改 → 两个都 `true`；都没改 → 两个都 `false`

这两个值被放进任务的 `outputs`，**传给了后面的任务**：
```yaml
outputs:
  backend_changed:  ${{ steps.out.outputs.backend }}
  frontend_changed: ${{ steps.out.outputs.frontend }}
```

> 手动触发（`workflow_dispatch`）时没有"前后两次提交"可对比，所以代码里直接强制 `backend=true`、`frontend=true`，即"手动时前后端都发"。

### 第 3 步：用 `if` 门禁决定跑不跑子流程
下游两个任务各自带一个 `if` 条件，只有对应布尔值为 `true` 才执行，否则 **跳过（skipped）**：

```yaml
# 后端任务：只有 backend_changed == 'true' 才跑
if: ${{ needs.detect-changes.outputs.backend_changed == 'true' }}
uses: ./.github/workflows/deploy-backend.yml

# 前端任务：只有 frontend_changed == 'true' 才跑
if: ... frontend_changed == 'true' ...
uses: ./.github/workflows/deploy-frontend.yml
```

**所以"监听前端/后端改动并触发对应子流程"的本质就是：**
> 检测目录 → 输出布尔值 → 子任务用 `if` 门禁按布尔值决定是否执行。

---

## 4. 执行顺序与四种场景

任务之间用 `needs` 表达依赖（"必须等谁先跑完"）：

```yaml
deploy-backend-job:   needs: detect-changes                 # 后端等"检测"完
deploy-frontend-job:  needs: [detect-changes, deploy-backend-job]  # 前端等"检测"+("后端")
```

因为前端 `needs` 后端，所以 **前后端都改时，会先发后端、等后端跑完再发前端（串行）**。

| 场景 | backend_changed | frontend_changed | 实际执行 |
|---|---|---|---|
| 只改后端 | true | false | 只跑后端 |
| 只改前端 | false | true | 只跑前端 |
| 前后端都改 | true | true | 先后端，后前端 |
| 都没改 | false | false | 都不跑（两个任务都被跳过） |

### ⚠️ 一个容易踩的坑（skip cascade 级联跳过）
GitHub 的规则：**如果一个任务依赖的 job 被跳过，它自己也会被级联跳过。**
- 当"只改前端"时，后端任务因为 `if` 不成立 → 被跳过。
- 如果前端任务的 `if` 只写 `frontend_changed == 'true'`，它会因为"依赖的后端被跳过"而**也跟着跳过** → 前端永远发不出去！

解决办法就是前端任务的 `if` 里加 `always()` 和结果判断：
```yaml
if: >-
  always() &&                              # 即使有依赖被跳过，也继续评估本任务
  needs.detect-changes.outputs.frontend_changed == 'true' &&
  needs.deploy-backend-job.result != 'failure' &&    # 后端没失败
  needs.deploy-backend-job.result != 'cancelled'     # 后端没被取消
```
这样"只改前端"时：后端被跳过（result=skipped，不是 failure/cancelled）→ 前端仍然正常执行。

---

## 5. 为什么要有 `concurrency`（并发控制）

```yaml
concurrency:
  group: "pmd-deploy"
  cancel-in-progress: true
```

- 如果你短时间内连续 push 两次，会同时启动两条流水线。
- 它们都要读写**同一个 IIS 目录**，会互相抢文件 → Windows 报"文件被占用/锁定"。
- `group: "pmd-deploy"` 把同一类流水线归为一组；`cancel-in-progress: true` 表示
  **新的一次运行会取消掉正在跑的旧的一次**，保证同一时刻只有一条在动服务器。
- **Windows 自托管环境强烈建议保留。**

---

## 6. 名词小抄（给纯新手）

| 名词 | 意思 |
|---|---|
| workflow（工作流） | 一个 `.yml` 文件 = 一条流水线 |
| job（任务） | 流水线里的一个大步骤，比如"检测""部署前端" |
| step（步骤） | job 里的一条具体命令/一个 Action |
| runner | 真正干活的机器。这里用你自己的 Windows 机器（`self-hosted`） |
| `runs-on` | 指定用哪种 runner 来跑这个 job |
| `uses` | 调用一个现成 Action，或调用另一个工作流文件 |
| `needs` | "这个 job 必须等哪些 job 先跑完" |
| `if` | 条件成立才执行，否则跳过 |
| `outputs` | 一个 job 算出的结果，可被后面的 job 用 `needs.xxx.outputs` 读取 |
| `workflow_call` | 子工作流的触发方式，表示"我可以被主文件 `uses` 调用" |
| `concurrency` | 并发控制，防止多条流水线同时操作服务器 |

---

## 7. 新手常见问题

### Q1：界面上显示 "Waiting for a runner to pick up this job..."
表示 GitHub 已经触发了流水线，但**你的自托管 runner 没连上来接活**。
- 去 runner 机器上启动它：进入 runner 目录运行 `.\run.cmd`（前台）。
- 想长期在线：管理员运行 `.\svc.cmd install` + `.\svc.cmd start` 注册成 Windows 服务。
- 在 GitHub 仓库 **Settings → Actions → Runners** 里能看到 runner 是 `Idle` 还是 `Offline`。

### Q2：为什么 `fetch-depth: 0`？
因为要判断"这次改了哪些文件"需要对比上一次提交，必须拉完整历史（默认只拉最新一次）。

### Q3：手动触发时为什么前后端都发？
手动触发（`workflow_dispatch`）没有"前后两次提交"可供 diff，所以代码里直接把
`backend`/`frontend` 都强制为 `true`，方便你一键全量发布做调试。

### Q4：我只改了前端，后端任务会不会白跑？
不会。后端任务有 `if: backend_changed == 'true'`，没改后端它会被**跳过**，不消耗时间。

---

## 8. 想验证一下？

1. 在 `PMD_React` 里随便改一个文件 → push 到 main → 看 Actions 页面：应只跑"部署前端"。
2. 在 `PMD_Backend` 里改一个文件 → push → 应只跑"部署后端"。
3. 两个目录都改 → push → 应先后端、后前端（前端任务会显示"等待 deploy-backend-job"）。
4. 在 Actions 页面手动点 **Run workflow** → 前后端都应跑。

---

> 提示：真正的前端构建/发布命令在 `deploy-frontend.yml`，后端发布步骤待补充在 `deploy-backend.yml`。
