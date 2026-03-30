import { toast } from 'sonner';
import { cn } from '@/lib/utils';
import { notificationApi } from '@/lib/api';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Switch } from '@/components/ui/switch';
import { ScrollArea } from '@/components/ui/scroll-area';
import { NotificationsSkeleton } from './NotificationsSkeleton';
import { useMemo, useEffect, useState, useCallback } from 'react';
import type { Notification } from '@/components/types/notification';
import { Trash2, BellDotIcon, AlertTriangleIcon } from 'lucide-react';
import { isTaskNotification, isWebhookFailureNotification } from '@/lib/notification-utils';
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from '@/components/ui/empty';


const getBadgeColor = (type: string) => {
  switch (type) {
    case 'error': return 'text-red-500 dark:text-red-400 border-red-500 dark:border-red-400';
    case 'success': return 'text-green-500 dark:text-green-400 border-green-500 dark:border-green-400';
    case 'warning': return 'text-amber-500 dark:text-amber-400 border-amber-500 dark:border-amber-400';
    case 'info':
    default: return 'text-blue-500 dark:text-blue-400 border-blue-500 dark:border-blue-400';
  }
};

const formatTimestamp = (timestamp: string) => {
  const date = new Date(timestamp);
  return date.toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const formatMessageWithTruncatedId = (message: string): string => {
  return message.replace(/\b([a-f0-9]{64})\b/g, (match) => {
    if (match.length <= 16) return match;
    return `${match.slice(0, 8)}...${match.slice(-8)}`;
  });
};

const NOTIFICATIONS_EVENT = 'notificationsUpdated';

const notifyError = (fallbackMessage: string, err: unknown) => {
  toast.error(err instanceof Error ? err.message : fallbackMessage, { position: 'top-center' });
};

interface NotificationItemProps {
  notification: Notification;
  isSelected: boolean;
  onSelect: (notification: Notification) => void;
  onDelete: (id: string, e: React.MouseEvent) => void;
}

function NotificationItem({ notification, isSelected, onSelect, onDelete }: NotificationItemProps) {
  return (
    <div
      className={cn(
        'group cursor-pointer rounded-lg border p-3 text-sm transition-all hover:bg-accent/70',
        isSelected && 'bg-accent/70'
      )}
      onClick={() => onSelect(notification)}
    >
      <div className="flex w-full flex-col gap-2">
        <div className="flex flex-col items-start gap-2 md:flex-row md:items-center md:justify-between">
          <div className="flex w-full items-center gap-3 md:w-auto">
            <div className="font-semibold">{notification.title}</div>
            <Badge variant="outline" className={getBadgeColor(notification.type)}>
              {notification.type}
            </Badge>
            {!notification.read && <span className="h-2 w-2 rounded-full bg-blue-600" />}
          </div>
          <div className="flex w-full items-center justify-between gap-2 md:w-auto">
            <div className={cn(isSelected ? 'text-foreground' : 'text-muted-foreground')}>
              {formatTimestamp(notification.createdAt)}
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="h-6 w-6 opacity-0 transition-opacity group-hover:opacity-100"
              onClick={(e) => onDelete(notification.id, e)}
              aria-label="Delete notification"
            >
              <Trash2 className="h-3 w-3" />
            </Button>
          </div>
        </div>

        <div className="line-clamp-2 text-muted-foreground">
          {formatMessageWithTruncatedId(notification.message)}
        </div>

        {isTaskNotification(notification.title) && (
          <span className="mt-1 block text-sm text-muted-foreground">task-level event</span>
        )}

        {isWebhookFailureNotification(notification.title) && (
          <span className="mt-1 flex items-center gap-1 text-xs font-medium text-destructive">
            <AlertTriangleIcon className="h-3 w-3" />
            webhook integration affected
          </span>
        )}
      </div>
    </div>
  );
}

export function NotificationsPage() {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [selectedNotification, setSelectedNotification] = useState<Notification | null>(null);
  const [hideTaskEvents, setHideTaskEvents] = useState(false);
  const [loading, setLoading] = useState(true);

  const visibleNotifications = useMemo(
    () => (hideTaskEvents ? notifications.filter((n) => !isTaskNotification(n.title)) : notifications),
    [hideTaskEvents, notifications]
  );

  const hasUnread = useMemo(() => notifications.some((n) => !n.read), [notifications]);

  const fetchNotifications = useCallback(async () => {
    setLoading(true);
    try {
      const data = await notificationApi.getAll();
      setNotifications(data);
    } catch (err) {
      notifyError('Failed to load notifications', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchNotifications();
    const interval = setInterval(fetchNotifications, 30000);
    return () => clearInterval(interval);
  }, [fetchNotifications]);

  const handleMarkAsRead = useCallback(async (id: string) => {
    try {
      await notificationApi.markAsRead(id);
      setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, read: true } : n)));
      window.dispatchEvent(new CustomEvent(NOTIFICATIONS_EVENT));
    } catch (err) {
      notifyError('Failed to mark as read', err);
    }
  }, []);

  const handleMarkAllAsRead = useCallback(async () => {
    try {
      await notificationApi.markAllAsRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
      window.dispatchEvent(new CustomEvent(NOTIFICATIONS_EVENT));
    } catch (err) {
      notifyError('Failed to mark all as read', err);
    }
  }, []);

  const handleDelete = useCallback(async (id: string, e: React.MouseEvent) => {
    e.stopPropagation();
    try {
      await notificationApi.delete(id);
      setNotifications((prev) => prev.filter((n) => n.id !== id));
      setSelectedNotification((prev) => (prev?.id === id ? null : prev));
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to delete notification';
      if (message.includes('not found')) {
        setNotifications((prev) => prev.filter((n) => n.id !== id));
        setSelectedNotification((prev) => (prev?.id === id ? null : prev));
      } else {
        toast.error(message, { position: 'top-center' });
      }
    }
  }, []);

  const handleSelectNotification = useCallback(
    (notification: Notification) => {
      setSelectedNotification(notification);
      if (!notification.read) {
        void handleMarkAsRead(notification.id);
      }
    },
    [handleMarkAsRead]
  );

  if (loading) {
    return (
      <ScrollArea className="h-[calc(100vh-var(--header-height))]">
        <NotificationsSkeleton />
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-[calc(100vh-var(--header-height))]">
      <div className="flex items-center justify-between p-4">
        {visibleNotifications.length > 0 && (
          <h1 className="text-2xl font-bold">Notifications</h1>
        )}
        {hasUnread && (
          <Button onClick={handleMarkAllAsRead} variant="outline">
            Mark All as Read
          </Button>
        )}
      </div>

      {notifications.length > 0 && (
        <div className="flex items-center gap-2 text-sm text-muted-foreground px-4 pb-5">
          <Switch
            id="hide-task-events"
            checked={hideTaskEvents}
            onCheckedChange={setHideTaskEvents}
          />
          <label htmlFor="hide-task-events">Hide per-task events</label>
        </div>
      )}

      {visibleNotifications.length === 0 ? (
        <Empty>
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <BellDotIcon />
            </EmptyMedia>
            <EmptyTitle>Notifications</EmptyTitle>
            <EmptyDescription>
              When something happens, you’ll see it here.
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      ) : (
        <div className="flex flex-col gap-3 p-4 pt-0 w-full lg:w-3/4">
          {visibleNotifications.map((notification) => (
            <NotificationItem
              key={notification.id}
              notification={notification}
              isSelected={selectedNotification?.id === notification.id}
              onSelect={handleSelectNotification}
              onDelete={handleDelete}
            />
          ))}
        </div>
      )}
    </ScrollArea>
  );
}