"use client"

import * as React from "react"
import { Link, matchPath, useLocation } from "react-router-dom"
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
    BookOpen,
} from "lucide-react"

type NavItem = {
    title: string
    to: string
    icon: React.ElementType
    matchPathPattern: string
    end?: boolean
}

const data = {
    navMain: [
        {
            title: "Storage",
            to: "/storage",
            icon: HardDrive,
            matchPathPattern: "/storage",
            end: true,
        },
        {
            title: "Knowledge Base",
            to: "/knowledge-base",
            icon: BookOpen,
            matchPathPattern: "/knowledge-base",
            end: true,
        },
        {
            title: "Storage Demo",
            to: "/storage/demo",
            icon: HardDrive,
            matchPathPattern: "/storage/demo",
            end: true,
        },
    ] satisfies NavItem[],
}

function NavMain({ items }: { items: NavItem[] }) {
    const { pathname } = useLocation()

    return (
        <SidebarGroup>
            <SidebarGroupLabel>Platform</SidebarGroupLabel>
            <SidebarGroupContent>
                <SidebarMenu>
                    {items.map((item) => {
                        const isActive = !!matchPath({ path: item.matchPathPattern, end: item.end ?? true }, pathname)

                        return (
                            <SidebarMenuItem key={item.title}>
                                <SidebarMenuButton asChild isActive={isActive} tooltip={item.title}>
                                    <Link to={item.to}>
                                    {item.icon && <item.icon />}
                                    <span>{item.title}</span>
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
