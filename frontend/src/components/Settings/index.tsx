import { toast } from 'sonner';
import { settingsApi } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Label } from '@/components/ui/label';
import { Switch } from "@/components/ui/switch";
import { SettingsSkeleton } from './SettingsSkeleton';
import { useTheme } from '@/components/ThemeProvider';
import { Separator } from "@/components/ui/separator";
import { ScrollArea } from "@/components/ui/scroll-area";
import type { UserPreferences } from '@/components/types/preferences'; 
import { CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {  Select, SelectContent,  SelectGroup,  SelectItem,  SelectLabel,  SelectTrigger,  SelectValue } from "@/components/ui/select";


export default function SettingsPage() {
  const { theme, setTheme } = useTheme();
  const [preferences, setPreferences] = useState<UserPreferences>({
    theme: 'system',
    notifications: {
      email: true,
      push: false,
      executionComplete: true,
      executionFailed: true,
    },
    language: 'en',
    timezone: 'UTC',
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchPreferences();
  }, []);


  const fetchPreferences = async () => {
    setLoading(true);
    try {
      const data = await settingsApi.getPreferences();
      setPreferences(data);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to load preferences', { position: 'top-center' });
    } finally {
      setLoading(false);
    }
  };

  const handleThemeChange = async (value: string) => {
    if (value === 'light' || value === 'dark' || value === 'system') {
      setTheme(value);
      try {
        await settingsApi.updatePreferences({ ...preferences, theme: value });
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to update theme', { position: 'top-center' });
      }
    }
  };

  const handleNotificationToggle = async (key: keyof UserPreferences['notifications'], value: boolean) => {
    const updated = { ...preferences, notifications: { ...preferences.notifications, [key]: value } };
    setPreferences(updated);
    try {
      await settingsApi.updatePreferences(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update notification settings', { position: 'top-center' });
    }
  };

  const handleLanguageChange = async (value: string) => {
    const updated = { ...preferences, language: value };
    setPreferences(updated);
    try {
      await settingsApi.updatePreferences(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update language', { position: 'top-center' });
    }
  };

  const handleTimezoneChange = async (value: string) => {
    const updated = { ...preferences, timezone: value };
    setPreferences(updated);
    try {
      await settingsApi.updatePreferences(updated);
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update timezone', { position: 'top-center' });
    }
  };

  if (loading) return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <SettingsSkeleton />
    </ScrollArea>
  );

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <CardHeader className="pl-4">
        <CardTitle>Appearance</CardTitle>
      </CardHeader>

      <CardContent className="space-y-6 mb-4">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-1 "> 
              <p className="text-muted-foreground text-sm">Customize how the application looks</p>
            </div>
            
            <Select value={theme} onValueChange={handleThemeChange}>
              <SelectTrigger className="w-full max-w-36">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectLabel>Theme</SelectLabel>
                  <SelectItem value="dark">Dark</SelectItem>
                  <SelectItem value="light">Light</SelectItem>
                  <SelectItem value="system">System</SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
        </div>
      </CardContent>

      <Separator />

      <CardHeader className="pl-4">
        <CardTitle>Notification Preferences</CardTitle>
        <CardDescription>Choose what notifications you want to receive.</CardDescription>
      </CardHeader>

      <CardContent className="space-y-6 mb-4">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <Label className="text-base">Email Notifications</Label>
              <p className="text-muted-foreground text-sm">Receive email alerts for important system events and updates</p>
            </div>
            <Switch 
              checked={preferences.notifications.email}
              onCheckedChange={(checked) => handleNotificationToggle('email', checked)}
              aria-label="Toggle email notifications"
            />
          </div>

          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <Label className="text-base">Execution Complete</Label>
              <p className="text-muted-foreground text-sm">Get notified when agent executions finish successfully</p>
            </div>
            <Switch 
              checked={preferences.notifications.executionComplete}
              onCheckedChange={(checked) => handleNotificationToggle('executionComplete', checked)}
              aria-label="Toggle execution complete notifications"
            />
          </div>

          <div className="flex items-center justify-between gap-4">
            <div className="space-y-1 flex-1 min-w-0">
              <Label className="text-base">Execution Failed</Label>
              <p className="text-muted-foreground text-sm">Receive alerts when agent executions encounter errors or failures</p>
            </div>
            <Switch 
              checked={preferences.notifications.executionFailed}
              onCheckedChange={(checked) => handleNotificationToggle('executionFailed', checked)}
              aria-label="Toggle execution failed notifications"
              className="flex-shrink-0"
            />
          </div>
        </div>
      </CardContent>

      <Separator />

      <CardHeader className="pl-4">
        <CardTitle>Localization</CardTitle>
        <CardDescription>Set your language and timezone preferences.</CardDescription>
      </CardHeader>

      <CardContent className="space-y-6 mb-4">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-1">
              <Label className="text-base">Language</Label>
              <p className="text-muted-foreground text-sm">Select your preferred display language</p>
            </div>
            
            <Select value={preferences.language} onValueChange={handleLanguageChange}>
              <SelectTrigger className="w-full max-w-36">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectLabel>Language</SelectLabel>
                  <SelectItem value="en">English</SelectItem>
                  <SelectItem value="es">Spanish</SelectItem>
                  <SelectItem value="fr">French</SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
        </div>
      </CardContent>

      <CardContent className="space-y-6 mb-4">
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="space-y-1 mr-4">
              <Label className="text-base">Timezone</Label>
              <p className="text-muted-foreground text-sm">Choose your local timezone for accurate timestamps</p>
            </div>
            
            <Select value={preferences.timezone} onValueChange={handleTimezoneChange}>
              <SelectTrigger className="w-full max-w-36">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectGroup>
                  <SelectLabel>Timezone</SelectLabel>
                  <SelectItem value="UTC">UTC</SelectItem>
                  <SelectItem value="America/New_York">Eastern Time</SelectItem>
                  <SelectItem value="America/Los_Angeles">Pacific Time</SelectItem>
                  <SelectItem value="Europe/London">London</SelectItem>
                </SelectGroup>
              </SelectContent>
            </Select>
          </div>
        </div>
      </CardContent>
    </ScrollArea>
  );
}