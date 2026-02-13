import { HashRouter, Routes, Route } from 'react-router-dom'
import RequireAuth from '@/components/RequireAuth'
import MainLayout from '@/components/layouts/MainLayout'
import AuthLayout from '@/components/layouts/AuthLayout'
import Home from '@/pages/Home'
import Login from '@/pages/Login'
import './App.css'

function App() {
  return (
    <HashRouter>
      <Routes>
        <Route element={<RequireAuth />}>
          <Route element={<MainLayout />}>
            <Route path="/" element={<Home />} />
          </Route>
        </Route>
        <Route element={<AuthLayout />}>
          <Route path="/login" element={<Login />} />
        </Route>
      </Routes>
    </HashRouter>
  )
}

export default App

