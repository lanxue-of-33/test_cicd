import { useState } from 'react'

function App() {
  const [count, setCount] = useState(0)
  const [name, setName] = useState('')
  const [dark, setDark] = useState(false)

  return (
    <div
      style={{
        background: dark ? '#1e1e1e' : '#f6f8fa',
        color: dark ? '#f0f0f0' : '#213547',
        borderRadius: '12px',
        padding: '2rem',
        transition: 'all 0.3s ease',
      }}
    >
      <h1>PMD_React 测试页面</h1>
      <p>这是一个使用 React + TypeScript + Vite 搭建的演示页面。</p>

      {/* 计数器 */}
      <section style={{ margin: '1.5rem 0' }}>
        <h2>计数器</h2>
        <button onClick={() => setCount((c) => c - 1)} style={btnStyle}>
          -
        </button>
        <span style={{ margin: '0 1rem', fontSize: '1.5rem', minWidth: '2rem', display: 'inline-block' }}>
          {count}
        </span>
        <button onClick={() => setCount((c) => c + 1)} style={btnStyle}>
          +
        </button>
      </section>

      {/* 输入框回显 */}
      <section style={{ margin: '1.5rem 0' }}>
        <h2>输入框回显</h2>
        <input
          value={name}
          onChange={(e) => setName(e.target.value)}
          placeholder="请输入你的名字"
          style={{
            padding: '0.5rem 0.75rem',
            borderRadius: '6px',
            border: '1px solid #ccc',
            fontSize: '1rem',
            width: '60%',
          }}
        />
        <p>{name ? `你好，${name}！👋` : '等待输入...'}</p>
      </section>

      {/* 主题切换 */}
      <section style={{ margin: '1.5rem 0' }}>
        <h2>主题切换</h2>
        <button onClick={() => setDark((d) => !d)} style={btnStyle}>
          {dark ? '切换到浅色' : '切换到深色'}
        </button>
      </section>
    </div>
  )
}

const btnStyle: React.CSSProperties = {
  padding: '0.5rem 1.25rem',
  margin: '0 0.25rem',
  fontSize: '1.25rem',
  borderRadius: '8px',
  border: 'none',
  cursor: 'pointer',
  background: '#646cff',
  color: '#fff',
}

export default App
