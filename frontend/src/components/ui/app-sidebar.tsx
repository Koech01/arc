"use client";
import * as React from "react";
import { auth } from "@/lib/api";
import { NavMain } from "@/components/ui/nav-main";
import { NavUser } from "@/components/ui/nav-user";
import { NavSecondary } from "@/components/ui/nav-secondary";
import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem } from "@/components/ui/sidebar";
import { ArrowUpCircleIcon, CircleFadingPlus, DatabaseZapIcon, CopyCheckIcon, BellDotIcon, LayoutDashboardIcon, GitPullRequestArrowIcon, SettingsIcon, CirclePlayIcon, WebhookIcon, CpuIcon, ShieldIcon } from "lucide-react";


const data = {
  navMain: [
    {
      title: "Dashboard",
      url: "/dashboard",
      icon: LayoutDashboardIcon,
    },
    {
      title: "Workflows",
      url: "/workflows",
      icon: GitPullRequestArrowIcon,
    },
    {
      title: "Create Workflow",
      url: "/workflows/create",
      icon: CircleFadingPlus,
    },
    {
      title: "Executions",
      url: "/executions",
      icon: CirclePlayIcon,
    },
    {
      title: "Webhooks",
      url: "/webhooks",
      icon: WebhookIcon,
    },
    {
      title: "LLM Providers",
      url: "/llms",
      icon: CpuIcon,
    },
    {
      title: "Data Persistence",
      url: "/data-persistence",
      icon: DatabaseZapIcon,
    },
    {
      title: "Templates",
      url: "/templates",
      icon: CopyCheckIcon,
    },
    {
      title: "Notifications",
      url: "/notifications",
      icon: BellDotIcon,
    }
  ],
  navSecondary: [
    {
      title: "Settings",
      url: "/settings",
      icon: SettingsIcon,
    }
  ]
}


export function AppSidebar({ ...props }: React.ComponentProps<typeof Sidebar>) {
  const [user, setUser] = React.useState({ name: "", email: "", avatar: "", role: "" });

  React.useEffect(() => {
    auth.checkAuth().then(data => {
      setUser({ name: data.username, email: data.email, avatar: "", role: data.role });
    }).catch(() => {});
  }, []);

  const navSecondary = [
    ...(user.role === "Admin" ? [{
      title: "Admin",
      url: "/admin",
      icon: ShieldIcon,
    }] : []),
    {
      title: "Settings",
      url: "/settings",
      icon: SettingsIcon,
    }
  ];

  return (
    <Sidebar collapsible="offcanvas" {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton
              asChild
              className="data-[slot=sidebar-menu-button]:!p-1.5"
            >
              <a href="#">
                <ArrowUpCircleIcon className="h-5 w-5" />
                <span className="text-base font-semibold">Arc.</span>
              </a>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent>
        <NavMain items={data.navMain} />
        <NavSecondary items={navSecondary} className="mt-auto" />
      </SidebarContent>
      <SidebarFooter>
        <NavUser user={user} />
      </SidebarFooter>
    </Sidebar>
  )
}