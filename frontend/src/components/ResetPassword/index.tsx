import { ResetPasswordForm } from "@/components/ResetPassword/reset-password-form";


const ResetPassword = () => {
  return (
    <div className="flex min-h-screen w-full items-center justify-center p-6 md:p-10">
      <div className="w-full max-w-sm">
        <ResetPasswordForm />
      </div>
    </div>
  );
};

export default ResetPassword;