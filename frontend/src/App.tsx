import './App.css';
import Login from './components/Login';
import Signup from './components/SignUp';
import Page from './components/Home/page'; 
import Dashboard from './components/Dashboard';
import ProfilePage from './components/Profile';
import AdminDashboard from './components/Admin';
import SettingsPage from './components/Settings';
import Executions from './components/Executions';
import { Toaster } from '@/components/ui/sonner';
import CreateWorkflow from './components/Workflows';
import { NotFoundPage } from './components/NotFound';
import OnboardingPage from './components/Onboarding';
import WebhooksListPage from './components/Webhooks';
import ResetPassword from './components/ResetPassword';
import ForgotPassword from './components/ForgotPassword';
import QuickExecutePage from './components/QuickExecute';
import LLMManagementPage from './components/LLMManagement';
import { ExecutionListPage } from './components/Executions';
import { ProtectedRoute } from './components/ProtectedRoute';
import BatchExecutionPage from './components/BatchExecution';
import WebhooksAdmin from './components/Admin/WebhooksAdmin';
import AdminAuditLog from './components/Admin/AdminAuditLog';
import RegressionGatesPage from './components/RegressionGates';
import DataPersistencePage from './components/DataPersistence';
import CacheManagementPage from './components/CacheManagement';
import { NotificationsPage } from './components/Notifications';
import UserManagement from './components/Admin/UserManagement';
import { RequireAdmin } from './components/Admin/RequireAdmin';
import LLMConfigsAdmin from './components/Admin/LLMConfigsAdmin';
import CacheManagement from './components/Admin/CacheManagement';
import MaintenanceMode from './components/Admin/MaintenanceMode';
import SystemExecutions from './components/Admin/SystemExecutions';
import SystemConfiguration from './components/Admin/SystemConfiguration';
import { PerformanceProfilePage } from './components/PerformanceProfile';
import { ReplayExecution } from './components/Executions/ReplayExecution';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { WorkflowListPage, WorkflowDetailPage } from './components/Workflows';
import { CompareExecutions } from './components/Executions/CompareExecutions';
import { TemplateListPage, CreateTemplatePage, EditTemplatePage, InstantiateTemplatePage } from './components/Templates';


function App() {
  return (
    <div className="App">
      <BrowserRouter>
        <Routes>
          <Route path='/login/' element={<Login/>}/>
          <Route path='/signup/' element={<Signup/>}/>
          <Route path='/forgot/' element={<ForgotPassword/>}/>
          <Route path='/reset/:token/' element={<ResetPassword/>}/>
          <Route path='/onboarding' element={<OnboardingPage/>}/>
          <Route path='/' element={
            <ProtectedRoute>
              <Page/>
            </ProtectedRoute>
          }>
            <Route index element={<Navigate to='/dashboard' replace />}/>
            <Route path='dashboard' element={<Dashboard/>}/>
            <Route path='executions' element={<ExecutionListPage/>}/>
            <Route path='executions/compare' element={<CompareExecutions/>}/>
            <Route path='executions/:id' element={<Executions/>}/>
            <Route path='executions/:id/replay' element={<ReplayExecution/>}/>
            <Route path='executions/:id/profile' element={<PerformanceProfilePage/>}/>
            <Route path='workflows' element={<WorkflowListPage/>}/>
            <Route path='workflows/create' element={<CreateWorkflow/>}/>
            <Route path='workflows/:id' element={<WorkflowDetailPage/>}/>
            <Route path='webhooks' element={<WebhooksListPage/>}/>
            <Route path='data-persistence' element={<DataPersistencePage/>}/>
            <Route path='cache' element={<CacheManagementPage/>}/>
            <Route path='batch' element={<BatchExecutionPage/>}/>
            <Route path='profile' element={<ProfilePage/>}/>
            <Route path='execute' element={<QuickExecutePage/>}/>
            <Route path='templates' element={<TemplateListPage/>}/>
            <Route path='templates/new' element={<CreateTemplatePage/>}/>
            <Route path='templates/:name/edit' element={<EditTemplatePage/>}/>
            <Route path='templates/:name/use' element={<InstantiateTemplatePage/>}/>
            <Route path='notifications' element={<NotificationsPage/>}/>
            <Route path='settings' element={<SettingsPage/>}/>
            <Route path='llms' element={<LLMManagementPage/>}/>
            <Route path='regression-gates' element={<RegressionGatesPage/>}/>
            
            {/* Admin Routes */}
            <Route path='admin' element={<RequireAdmin><AdminDashboard/></RequireAdmin>}/>
            <Route path='admin/users' element={<RequireAdmin><UserManagement/></RequireAdmin>}/>
            <Route path='admin/executions' element={<RequireAdmin><SystemExecutions/></RequireAdmin>}/>
            <Route path='admin/llm-configs' element={<RequireAdmin><LLMConfigsAdmin/></RequireAdmin>}/>
            <Route path='admin/webhooks' element={<RequireAdmin><WebhooksAdmin/></RequireAdmin>}/>
            <Route path='admin/cache' element={<RequireAdmin><CacheManagement/></RequireAdmin>}/>
            <Route path='admin/maintenance' element={<RequireAdmin><MaintenanceMode/></RequireAdmin>}/>
            <Route path='admin/system' element={<RequireAdmin><SystemConfiguration/></RequireAdmin>}/>
            <Route path='admin/audit-log' element={<RequireAdmin><AdminAuditLog/></RequireAdmin>}/>
            <Route path='*' element={<NotFoundPage/>}/>
          </Route>
          <Route path='*' element={<NotFoundPage/>}/>
        </Routes>
      </BrowserRouter>
      <Toaster position="top-center" />
    </div>
  );
}

export default App;