import { toast } from 'sonner';
import { auth } from '@/lib/api';
import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ProfileSkeleton } from './ProfileSkeleton';
import { Avatar,  AvatarFallback } from "@/components/ui/avatar";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';


interface UserProfile {
  userId: string;
  username: string;
  email: string;
  role: string;
  firstname?: string;
}

export default function ProfilePage() {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState(false);
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [firstname, setFirstname] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const data = await auth.checkAuth();
        setUser(data);
        setUsername(data.username);
        setEmail(data.email);
        setFirstname(data.firstname || '');
      } catch (err) {
        toast.error(err instanceof Error ? err.message : 'Failed to load profile', { position: 'top-center' });
      } finally {
        setLoading(false);
      }
    };

    fetchProfile();
  }, []);

  const handleRetry = () => {
    setLoading(true);
    auth.checkAuth()
      .then(data => {
        setUser(data);
        setUsername(data.username);
        setEmail(data.email);
        setFirstname(data.firstname || '');
      })
      .catch(err => toast.error(err instanceof Error ? err.message : 'Failed to load profile', { position: 'top-center' }))
      .finally(() => setLoading(false));
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const updated = await auth.updateProfile({ username, email, firstname });
      setUser(updated);
      setEditing(false);
      toast.success('Profile updated successfully', { position: 'top-center' });
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'Failed to update profile', { position: 'top-center' });
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    setUsername(user?.username || '');
    setEmail(user?.email || '');
    setFirstname(user?.firstname || '');
    setEditing(false);
  };

  if (loading) {
    return <ProfileSkeleton />;
  }

  if (!user) {
    return (
      <div className="p-6">
        <p className="text-muted-foreground">Failed to load profile</p>
        <Button onClick={handleRetry} className="mt-4">Retry</Button>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      <Card className="pb-4">
        <CardHeader> 
          <div className="flex items-center gap-6 md:gap-4Log out of your account? py-1.5 text-left">
            <Avatar className="h-12 w-12 rounded-lg"> 
              <AvatarFallback className="rounded-lg">{username.charAt(0).toUpperCase()}</AvatarFallback>
            </Avatar>

            <div className="grid flex-1 text-left"> 
              <CardTitle className="text-lg">Account Settings</CardTitle>
              <CardDescription>Manage your account settings and preferences</CardDescription> 
            </div>
          </div> 
        </CardHeader>

        <CardContent>
          {editing ? (
            <form onSubmit={handleSave} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="px-2 md:px-0">
                  <Label htmlFor="firstname">First Name</Label>
                  <Input
                    id="firstname"
                    type="text"
                    value={firstname}
                    onChange={(e) => setFirstname(e.target.value)}
                    placeholder="John"
                    aria-label="First name"
                  />
                </div>
                <div className="px-2 md:px-0">
                  <Label htmlFor="username">Username</Label>
                  <Input
                    id="username"
                    type="text"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                    required
                    aria-label="Username"
                    aria-required="true"
                  />
                </div>
                <div className="px-2 md:px-0">
                  <Label htmlFor="email">Email</Label>
                  <Input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                    aria-label="Email address"
                    aria-required="true"
                  />
                </div>
                <div className="px-2 md:px-0">
                  <Label htmlFor="role">Role</Label>
                  <div className="mt-2">
                    <Badge 
                      variant="outline" 
                      className={user.role === 'Admin' ? 'text-purple-600 border-purple-500' : 'text-emerald-500 border-emerald-500'}
                    >
                      {user.role}
                    </Badge>
                  </div>
                </div>
              </div>
              <div className="flex gap-2">
                <Button type="submit" disabled={saving} aria-busy={saving}>
                  {saving ? 'Saving...' : 'Save Changes'}
                </Button>
                <Button type="button" variant="outline" onClick={handleCancel} disabled={saving}>
                  Cancel
                </Button>
              </div>
            </form>
          ) : (
            <div className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="px-2 md:px-0">
                  <label className="text-sm font-medium text-muted-foreground">First Name</label>
                  <p className="text-sm mt-1">{user.firstname || 'Not set'}</p>
                </div>
                <div className="px-2 md:px-0">
                  <label className="text-sm font-medium text-muted-foreground">Username</label>
                  <p className="text-sm mt-1">{user.username}</p>
                </div>
                <div className="px-2 md:px-0">
                  <label className="text-sm font-medium text-muted-foreground">Email</label>
                  <p className="text-sm mt-1">{user.email}</p>
                </div>
                <div className="px-2 md:px-0">
                  <label className="text-sm font-medium text-muted-foreground">Role</label>
                  <div className="mt-1">
                    <Badge 
                      variant="outline" 
                      className={user.role === 'Admin' ? 'text-purple-600 border-purple-500' : 'text-emerald-500 border-emerald-500'}
                    >
                      {user.role}
                    </Badge>
                  </div>
                </div>
              </div>
              <Button onClick={() => setEditing(true)}>Edit Profile</Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}