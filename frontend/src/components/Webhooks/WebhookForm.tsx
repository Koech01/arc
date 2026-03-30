import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { useState, type FormEvent } from 'react';
import { Checkbox } from '@/components/ui/checkbox';
import { Eye, EyeOff, RefreshCw, Copy } from 'lucide-react';
import { WEBHOOK_EVENTS } from '@/components/types/webhook';
import type { WebhookEventType } from '@/components/types/webhook';


interface WebhookFormSubmitData {
  url: string;
  events: WebhookEventType[];
  secret: string;
}

interface WebhookFormProps {
  onSubmit: (data: WebhookFormSubmitData) => Promise<void>;
  isLoading: boolean;
  onCancel: () => void;
  initialValues?: { url: string; events: WebhookEventType[] };
  mode?: 'create' | 'edit';
}

export function WebhookForm({ onSubmit, isLoading, onCancel, initialValues, mode = 'create' }: WebhookFormProps) {
  const [url, setUrl] = useState(initialValues?.url ?? '');
  const [secret, setSecret] = useState('');
  const [showSecret, setShowSecret] = useState(false);
  const [selectedEvents, setSelectedEvents] = useState<WebhookEventType[]>(initialValues?.events ?? []);
  const [secretCopied, setSecretCopied] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const isEdit = mode === 'edit';

  const handleCancel = () => {
    setErrors({});
    onCancel();
  };

  const generateSecret = () => {
    const length = 32;
    const charset =
      'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*';
    let result = '';
    for (let i = 0; i < length; i++) {
      result += charset.charAt(Math.floor(Math.random() * charset.length));
    }
    setSecret(result);
  };

  const copySecret = () => {
    navigator.clipboard.writeText(secret);
    setSecretCopied(true);
    setTimeout(() => setSecretCopied(false), 2000);
  };

  const validateUrl = (urlString: string): boolean => {
    if (!urlString) {
      setErrors((prev) => ({ ...prev, url: 'URL is required' }));
      return false;
    }
    try {
      const urlObj = new URL(urlString);
      if (!['http:', 'https:'].includes(urlObj.protocol)) {
        setErrors((prev) => ({ ...prev, url: 'Must be a valid HTTPS or HTTP URL' }));
        return false;
      }
      setErrors((prev) => { const n = { ...prev }; delete n.url; return n; });
      return true;
    } catch {
      setErrors((prev) => ({ ...prev, url: 'Enter a valid HTTPS or HTTP URL' }));
      return false;
    }
  };

  const validateSecret = (secretString: string): boolean => {
    if (!secretString) {
      setErrors((prev) => ({ ...prev, secret: 'Secret is required' }));
      return false;
    }
    if (secretString.length < 20) {
      setErrors((prev) => ({
        ...prev,
        secret: `Secret must be at least 20 characters (${secretString.length}/20)`,
      }));
      return false;
    }
    setErrors((prev) => { const n = { ...prev }; delete n.secret; return n; });
    return true;
  };

  const toggleEvent = (event: WebhookEventType) => {
    setSelectedEvents((prev) =>
      prev.includes(event) ? prev.filter((e) => e !== event) : [...prev, event]
    );
    if (selectedEvents.length > 0 || selectedEvents.includes(event)) {
      setErrors((prev) => { const n = { ...prev }; delete n.events; return n; });
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();

    if (!validateUrl(url)) return;

    // In edit mode, secret is optional (keep existing if blank); only validate if provided
    if (!isEdit && !validateSecret(secret)) return;
    if (isEdit && secret && !validateSecret(secret)) return;

    if (selectedEvents.length === 0) {
      setErrors((prev) => ({ ...prev, events: 'Select at least one event' }));
      return;
    }

    try {
      await onSubmit({ url, events: selectedEvents, secret });
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : `Failed to ${isEdit ? 'update' : 'register'} webhook`;
      setErrors({ submit: errorMessage });
    }
  };

  return (
    <form onSubmit={handleSubmit} className="space-y-6">
      {/* URL Field */}
      <div className="space-y-2">
        <label htmlFor="webhook-url" className="block text-sm font-medium">
          Webhook URL <span className="text-red-500">*</span>
        </label>
        <Input
          id="webhook-url"
          type="url"
          placeholder="https://example.com/webhooks/executions"
          value={url}
          onChange={(e) => setUrl(e.target.value)}
          onBlur={() => url && validateUrl(url)}
          disabled={isLoading}
          aria-invalid={!!errors.url}
          aria-describedby={errors.url ? 'url-error' : undefined}
          className="text-sm"
        />
        <p className="text-xs text-gray-500">
          Where we'll send execution notifications. Must use HTTPS for production.
        </p>
        {errors.url && (
          <div id="url-error" role="alert" className="text-sm text-red-600 flex items-center gap-1">
            <span>⚠</span> {errors.url}
          </div>
        )}
      </div>

      {/* Secret Field */}
      <div className="space-y-2">
        <label htmlFor="webhook-secret" className="block text-sm font-medium">
          Webhook Secret {!isEdit && <span className="text-red-500">*</span>}
        </label>
        <div className="flex gap-2">
          <div className="flex-1 relative">
            <Input
              id="webhook-secret"
              type={showSecret ? 'text' : 'password'}
              placeholder={isEdit ? 'Leave blank to keep existing secret' : 'Enter secret or auto-generate'}
              value={secret}
              onChange={(e) => setSecret(e.target.value)}
              disabled={isLoading}
              aria-invalid={!!errors.secret}
              aria-describedby={errors.secret ? 'secret-error' : undefined}
              className="text-sm pr-10"
            />
            <button
              type="button"
              onClick={() => setShowSecret(!showSecret)}
              className="absolute right-2 top-1/2 -translate-y-1/2 p-1 hover:bg-gray-100 rounded focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              aria-label={showSecret ? 'Hide secret' : 'Show secret'}
              disabled={isLoading}
            >
              {showSecret ? (
                <EyeOff className="h-4 w-4 text-gray-500" />
              ) : (
                <Eye className="h-4 w-4 text-gray-500" />
              )}
            </button>
          </div>
          <Button
            type="button"
            variant="outline"
            onClick={generateSecret}
            disabled={isLoading}
            title="Generate a random 32-character secret"
          >
            <RefreshCw className="h-4 w-4" />
          </Button>
        </div>

        <div className="flex justify-between items-start">
          <p className="text-xs text-gray-500">
            {isEdit
              ? 'Leave blank to keep existing secret. New secret used for HMAC-SHA256 signing.'
              : 'Used to sign webhook payloads (HMAC-SHA256). Min 20 characters.'}
          </p>
          {secret && (
            <div className="flex items-center gap-2">
              <span className={`text-xs font-medium ${secret.length >= 20 ? 'text-green-600' : 'text-red-600'}`}>
                {secret.length}/20
              </span>
              <button
                type="button"
                onClick={copySecret}
                className="inline-flex items-center justify-center rounded p-1 hover:bg-gray-100 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                aria-label="Copy secret to clipboard"
                disabled={isLoading}
              >
                <Copy className="h-3 w-3 text-gray-500" />
              </button>
              {secretCopied && (
                <p className="text-xs text-green-600" role="status" aria-live="polite">
                  Copied!
                </p>
              )}
            </div>
          )}
        </div>

        {errors.secret && (
          <div id="secret-error" role="alert" className="text-sm text-red-600 flex items-center gap-1">
            <span>⚠</span> {errors.secret}
          </div>
        )}
      </div>

      {/* Events Multi-Select */}
      <div className="space-y-3">
        <label className="block text-sm font-medium">
          Notification Events <span className="text-red-500">*</span>
        </label>
        <div className="space-y-3">
          {WEBHOOK_EVENTS.map((event) => (
            <div key={event.id} className="flex items-start gap-3">
              <Checkbox
                id={`event-${event.id}`}
                checked={selectedEvents.includes(event.id as WebhookEventType)}
                onCheckedChange={() => toggleEvent(event.id as WebhookEventType)}
                disabled={isLoading}
              />
              <label htmlFor={`event-${event.id}`} className="flex-1 cursor-pointer">
                <p className="text-sm font-medium">{event.label}</p>
                <p className="text-xs text-gray-500">{event.description}</p>
              </label>
            </div>
          ))}
        </div>

        {errors.events && (
          <div role="alert" className="text-sm text-red-600 flex items-center gap-1">
            <span>⚠</span> {errors.events}
          </div>
        )}
      </div>

      {/* Submit Error */}
      {errors.submit && (
        <div role="alert" className="text-sm text-red-600 bg-red-50 p-3 rounded flex items-center gap-2">
          <span>⚠</span> {errors.submit}
        </div>
      )}

      {/* Form Actions */}
      <div className="flex gap-3 justify-end pt-1">
        <Button type="button" variant="outline" onClick={handleCancel} disabled={isLoading}>
          Cancel
        </Button>
        <Button type="submit" disabled={isLoading} aria-busy={isLoading}>
          {isLoading
            ? isEdit ? 'Updating...' : 'Registering...'
            : isEdit ? 'Update Webhook' : 'Register Webhook'}
        </Button>
      </div>
    </form>
  );
}