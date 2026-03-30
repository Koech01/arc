import { ForgotPasswordForm } from "@/components/ForgotPassword/forgot-password-form";


const ForgotPassword = () => {
  return (
    <div className="flex min-h-screen w-full items-center justify-center p-6 md:p-10">
      <div className="w-full max-w-sm">
        <ForgotPasswordForm />
      </div>
    </div>
  );
};

export default ForgotPassword;