import { useState } from 'react'
import reactLogo from '@/assets/react.svg'
import viteLogo from '/electron-vite.animate.svg'
import { Button } from '@/components/ui/button'
import { SidebarTrigger } from "@/components/ui/sidebar"

export default function Home() {
  const [count, setCount] = useState(0)

  return (
    <>
      <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4">
        <SidebarTrigger className="-ml-1" />
        <div className="h-4 w-px bg-slate-200 mx-2" />
      </header>
      <main className="flex-1 flex flex-col items-center justify-center p-4">
        <div className="flex gap-4 mb-8">
          <a href="https://electron-vite.github.io" target="_blank">
            <img src={viteLogo} className="logo w-24 h-24" alt="Vite logo" />
          </a>
          <a href="https://react.dev" target="_blank">
            <img src={reactLogo} className="logo react w-24 h-24" alt="React logo" />
          </a>
        </div>
        <h1 className="text-4xl font-bold mb-4">Vite + React</h1>
        <div className="card p-4 border rounded-lg shadow-sm">
          <Button onClick={() => setCount((count) => count + 1)}>
            count is {count}
          </Button>
          <p className="mt-4 text-sm text-gray-500">
            Edit <code>src/App.tsx</code> and save to test HMR
          </p>
        </div>
        <p className="read-the-docs mt-8 text-gray-400">
          Click on the Vite and React logos to learn more
        </p>
      </main>
    </>
  )
}
