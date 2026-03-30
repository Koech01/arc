import { Outlet } from "react-router-dom";
import { SiteHeader } from "@/components/ui/site-header";
import { AppSidebar } from "@/components/ui/app-sidebar";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";


export default function Page() {
  return (
    <SidebarProvider>
      <AppSidebar variant="inset"/>
      <SidebarInset>
        <SiteHeader />
        <div className="flex flex-1 flex-col">
          <div className="@container/main flex flex-1 flex-col">
            <Outlet />
          </div>
        </div>
      </SidebarInset>
    </SidebarProvider>
  )
}