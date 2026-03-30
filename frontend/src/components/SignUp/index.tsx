import { SignupForm } from "@/components/SignUp/signup-form.tsx";


const Signup = () => {
   
  return (
    <div>
      <div className="flex min-h-screen w-full items-center justify-center p-6 md:p-10">
        <div className="w-full max-w-sm">
          <SignupForm />
        </div>
      </div>
    </div>
  );
};

export default Signup;