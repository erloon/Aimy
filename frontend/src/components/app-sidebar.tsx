"use client"

import * as React from "react"
import {
    Sidebar,
    SidebarContent,
    SidebarGroup,
    SidebarGroupContent,
    SidebarGroupLabel,
    SidebarHeader,
    SidebarMenu,
    SidebarMenuButton,
    SidebarMenuItem,
    SidebarRail,
} from "@/components/ui/sidebar"
import {
    HardDrive,
} from "lucide-react"

// This is sample data.
const data = {
    navMain: [
        {
            title: "Storage",
            url: "#/storage",
            icon: HardDrive,
            isActive: true,
        },
    ],
}

function NavMain({
    items,
}: {
    items: {
        title: string
        url: string
        icon?: React.ElementType
        isActive?: boolean
    }[]
}) {
    return (
        <SidebarGroup>
            <SidebarGroupLabel>Platform</SidebarGroupLabel>
            <SidebarGroupContent>
                <SidebarMenu>
                    {items.map((item) => (
                        <SidebarMenuItem key={item.title}>
                            <SidebarMenuButton asChild isActive={item.isActive} tooltip={item.title}>
                                <a href={item.url}>
                                    {item.icon && <item.icon />}
                                    <span>{item.title}</span>
                                </a>
                            </SidebarMenuButton>
                        </SidebarMenuItem>
                    ))}
                </SidebarMenu>
            </SidebarGroupContent>
        </SidebarGroup>
    )
}

export function AppSidebar({
    ...props
}: React.ComponentProps<typeof Sidebar>) {
    return (
        <Sidebar collapsible="icon" {...props}>
            <SidebarHeader>
                <div className="flex h-12 items-center px-4 group-data-[collapsible=icon]:px-0 group-data-[collapsible=icon]:justify-center">
                    <h2 className="text-lg font-semibold tracking-tight group-data-[collapsible=icon]:hidden">Aimy</h2>
                    <h2 className="hidden text-lg font-semibold tracking-tight group-data-[collapsible=icon]:block">A</h2>
                </div>
            </SidebarHeader>
            <SidebarContent>
                <NavMain items={data.navMain} />
            </SidebarContent>
            <SidebarRail />
        </Sidebar>
    )
}
