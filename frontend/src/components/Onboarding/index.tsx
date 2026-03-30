"use client";
import { cn } from "@/lib/utils";
import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter, CardHeader } from "@/components/ui/card";
import { ChevronLeft, ChevronRight, Check, Origami, Network, Satellite } from "lucide-react";


const steps = [
  {
    id: 1,
    title: "Design Intelligent Workflows",
    indicatorTitle: "Workflows",
    description:
      "Design powerful workflows where AI agents execute in perfect harmony. Chain tasks, set dependencies, and watch your automation symphony come alive.",
    icon: Origami,
    iconColor: "text-blue-600",
    iconBg: "bg-blue-100"
  },
  {
    id: 2,
    title: "Your AI Agents, One Team",
    indicatorTitle: "Coordination",
    description:
      "Stop juggling individual agents. Arc turns chaos into coordination where multiple AI models work together seamlessly toward your goals.",
    icon: Network,
    iconColor: "text-purple-600",
    iconBg: "bg-purple-100"
  },
  {
    id: 3,
    title: "See Everything, Control Everything",
    indicatorTitle: "Monitor",
    description:
      "Real-time execution graphs. Instant alerts. Complete visibility. Know exactly what every agent is doing, when it started, and when it finishes.",
    icon: Satellite,
    iconColor: "text-green-600",
    iconBg: "bg-green-100"
  }
];


export default function OnboardingPage() {
  const [currentStep, setCurrentStep] = useState(1);
  const navigate = useNavigate();
  const completionUrl = "/dashboard";

  const handleNext = () => {
    if (currentStep < steps.length) {
      setCurrentStep(currentStep + 1);
    }
  };

  const handlePrevious = () => {
    if (currentStep > 1) {
      setCurrentStep(currentStep - 1);
    }
  };

  const handleGetStarted = () => {
    navigate(completionUrl);
  };

  const renderStepContent = () => {
    const step = steps[currentStep - 1];
    const StepIcon = step.icon;

    switch (currentStep) {
      case 1:
      case 2:
      case 3:
        return (
          <div className="space-y-4">
            <Card className="w-full border-0 shadow-none">
              <CardContent className="flex w-full items-center justify-start gap-4 p-0 text-left">
                <div className="flex-shrink-0">
                  <div className={cn("flex h-12 w-12 items-center justify-center rounded-lg", step.iconBg)}>
                    <StepIcon className={cn("h-6 w-6", step.iconColor)} />
                  </div>
                </div>
                <div>
                  <h3 className="text-muted-foreground font-semibold">{step.title}</h3>
                </div>
              </CardContent>
            </Card>

            <p className="text-muted-foreground">{step.description}</p>
          </div>
        );

      default:
        return null;
    }
  };

  return (
    <div className="flex items-center justify-center p-4">
      <Card className="w-full max-w-3xl shadow-lg">
        <CardHeader className="pb-0">
          {/* Step Indicator */}
          <div className="mb-6 flex items-center justify-between">
            {steps.map((step) => (
              <div key={step.id} className="relative flex flex-1 flex-col items-center">
                <div
                  className={cn(
                    "flex h-10 w-10 items-center justify-center rounded-full text-sm font-semibold transition-colors duration-300",
                    currentStep > step.id
                      ? "bg-purple-600 text-white"
                      : currentStep === step.id
                        ? "bg-purple-500 text-white"
                        : "bg-gray-200 text-gray-600"
                  )}>
                  {currentStep > step.id ? <Check className="h-5 w-5" /> : step.id}
                </div>
                <div
                  className={cn(
                    "mt-2 text-center text-sm font-medium",
                    currentStep >= step.id ? "text-foreground" : "text-muted-foreground"
                  )}>
                  {step.indicatorTitle}
                </div>
                {step.id < steps.length && (
                  <div
                    className={cn(
                      "absolute top-5 left-[calc(50%+20px)] h-0.5 w-[calc(100%-40px)] -translate-y-1/2 bg-gray-200 transition-colors duration-300",
                      currentStep > step.id && "bg-purple-400"
                    )}
                  />
                )}
              </div>
            ))}
          </div>
        </CardHeader>

        <CardContent className="p-6 md:p-8">
          {renderStepContent()}
        </CardContent>

        {/* Navigation */}
        <CardFooter className="flex justify-between border-t">
          <Button variant="outline" onClick={handlePrevious} disabled={currentStep === 1}>
            <ChevronLeft className="h-4 w-4" />
            <span>Previous</span>
          </Button>

          {currentStep < steps.length ? (
            <Button onClick={handleNext}>
              <span>{currentStep === steps.length - 1 ? "Submit" : "Continue"}</span>
              <ChevronRight className="h-4 w-4" />
            </Button>
          ) : (
            <Button onClick={handleGetStarted}>
              <span>Get Started</span>
              <ChevronRight className="h-4 w-4" />
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  );
}