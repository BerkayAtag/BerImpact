import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './console.css'
import App from './Console.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
