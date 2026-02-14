import { HashRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClientProvider } from '@tanstack/react-query'
import RequireAuth from '@/components/RequireAuth'
import MainLayout from '@/components/layouts/MainLayout'
import AuthLayout from '@/components/layouts/AuthLayout'
import { StoragePage } from '@/features/storage/pages/StoragePage'
import Login from '@/pages/Login'
import { queryClient } from '@/lib/react-query'
import { Toaster } from '@/components/ui/toaster'
import './App.css'

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <HashRouter>
        <Routes>
          <Route element={<RequireAuth />}>
            <Route element={<MainLayout />}>
              <Route path="/" element={<Navigate to="/storage" replace />} />
              <Route path="/storage" element={<StoragePage />} />
            </Route>
          </Route>
          <Route element={<AuthLayout />}>
            <Route path="/login" element={<Login />} />
          </Route>
        </Routes>
      </HashRouter>
      <Toaster />
    </QueryClientProvider>
  )
}

export default App

