"use client";
import { useEffect, useState } from "react";
import { notificationApi } from "@/lib/api";
import { type LucideIcon } from "lucide-react";
import { Link, useLocation } from "react-router-dom";
import { isWebhookFailureNotification } from "@/lib/notification-utils";
import {  SidebarGroup, SidebarGroupContent,  SidebarMenu,  SidebarMenuButton,  SidebarMenuItem } from "@/components/ui/sidebar";


export function NavMain({
  items,
}: {
  items: {
    title: string
    url: string
    icon?: LucideIcon
  }[]
}) {
  const location = useLocation()
  const [unreadCount, setUnreadCount] = useState(0)
  const [hasWebhookFailure, setHasWebhookFailure] = useState(false)

  useEffect(() => {
    const fetchUnread = async () => {
      try {
        const notifications = await notificationApi.getAll()
        setUnreadCount(notifications.filter(n => !n.read).length)
        setHasWebhookFailure(
          notifications.some(n => !n.read && isWebhookFailureNotification(n.title))
        )
      } catch {
        setHasWebhookFailure(false)
      }
    }
    fetchUnread()
    const interval = setInterval(fetchUnread, 30000)
    
    const handleUpdate = () => fetchUnread()
    window.addEventListener('notificationsUpdated', handleUpdate)
    
    return () => {
      clearInterval(interval)
      window.removeEventListener('notificationsUpdated', handleUpdate)
    }
  }, [])

  return (
    <SidebarGroup>
      <SidebarGroupContent className="flex flex-col gap-2">
        <SidebarMenu>
          {items.map((item) => {
            const isActive = location.pathname === item.url
            const isNotifications = item.url === '/notifications'
            return (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton tooltip={item.title} asChild isActive={isActive}>
                  <Link to={item.url}>
                    {item.icon && <item.icon />}
                    <span>{item.title}</span>
                    {isNotifications && unreadCount > 0 && (
                      <span className="ml-auto flex h-2 w-2 rounded-full bg-blue-600" />
                    )}
                    {isNotifications && hasWebhookFailure && (
                      <span
                        className="flex h-2 w-2 rounded-full bg-destructive"
                        title="Webhook delivery failure"
                      />
                    )}
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            )
          })}
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  )
}