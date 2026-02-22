import { SidebarProvider, SidebarInset, SidebarTrigger } from "@/components/ui/sidebar"
import { AppSidebar } from "@/components/app-sidebar"
import { Outlet, matchPath, useLocation } from "react-router-dom"
import { Separator } from "@/components/ui/separator"

export default function MainLayout() {
  const { pathname } = useLocation()

  const pageTitle =
    (matchPath({ path: "/storage/demo", end: true }, pathname) && "Storage Demo") ||
    (matchPath({ path: "/knowledge-base", end: true }, pathname) && "Knowledge Base") ||
    (matchPath({ path: "/storage", end: true }, pathname) && "Storage") ||
    "Aimy"

  return (
    <SidebarProvider>
      <AppSidebar />
      <SidebarInset>
        <header className="flex h-16 shrink-0 items-center gap-2 border-b px-4 transition-[width,height] ease-linear group-has-[[data-collapsible=icon]]/sidebar-wrapper:h-12">
          <SidebarTrigger className="-ml-1" />
          <Separator orientation="vertical" className="mr-2 h-4" />
          <h2 className="text-lg font-semibold">{pageTitle}</h2>
        </header>
        <div className="flex flex-1 flex-col gap-4 p-4 pt-0 mt-4">
          <Outlet />
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}
