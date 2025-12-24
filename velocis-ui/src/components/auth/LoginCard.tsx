import React, { useEffect, useState, useCallback } from "react";
import { Button } from "@/components/ui/button";
import { signIn, signOut } from "@/lib/auth-client";
const GoogleIcon = () => (
  <svg
    width="20"
    height="20"
    viewBox="0 0 20 20"
    fill="none"
    xmlns="http://www.w3.org/2000/svg"
  >
    <path
      d="M19.9895 10.1871C19.9895 9.36767 19.9214 8.76973 19.7742 8.14966H10.1992V11.848H15.8328C15.5862 13.0628 14.687 14.9965 12.3572 16.6499L12.3353 16.8696L15.2691 19.1065L15.4727 19.1265C17.2917 17.4735 18.3492 15.0152 18.3492 12.112C18.3492 11.7583 18.3093 11.3985 18.239 11.0456"
      fill="#4285F4"
    />
    <path
      d="M10.1993 20.0001C12.9546 20.0001 15.2643 19.1111 16.9627 17.5523L14.0754 15.3153C13.1614 15.9383 11.979 16.317 10.1993 16.317C7.51472 16.317 5.24436 14.5369 4.43265 12.1374L4.22384 12.1545L1.17645 14.4716L1.10645 14.5684C2.81227 17.893 6.27315 20.0001 10.1993 20.0001"
      fill="#34A853"
    />
    <path
      d="M4.43236 12.1375C4.22129 11.5175 4.10397 10.8606 4.10397 10.1876C4.10397 9.51466 4.22129 8.85773 4.42621 8.23773L4.41624 8.01633L1.29853 5.63232L1.10615 5.80689C0.400511 7.18911 0 8.64724 0 10.1876C0 11.728 0.400511 13.1861 1.10615 14.5684L4.43236 12.1375"
      fill="#FBBC05"
    />
    <path
      d="M10.1993 4.05807C11.7011 4.05807 13.0487 4.56589 14.1061 5.56275L16.8797 2.83682C15.1437 1.23733 12.9546 0.375 10.1993 0.375C6.27315 0.375 2.81227 2.48208 1.10645 5.80693L4.42651 8.23777C5.24436 5.83827 7.51472 4.05807 10.1993 4.05807"
      fill="#EB4335"
    />
  </svg>
);

const LoginPage = () => {
  const handleSignIn = async () => {
    signIn.social({ provider: "google", callbackURL: "/" });
  };

  return (
    <div className="flex min-h-screen w-full font-sans">
      <div className="flex w-full flex-col justify-center px-8 lg:w-1/2 lg:px-20 xl:px-32 my-14">
        <div className="mx-auto w-full max-w-[440px]">
          <div className="mb-8">
            <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-3 flex items-center gap-2">
              Welcome Back <span className="text-3xl">👋</span>
            </h1>
            <p className="text-gray-500 dark:text-[rgba(255,255,255,0.62)] text-sm leading-relaxed">
              Start playing and enjoy with friends. Sign in to continue your
              gaming journey.
            </p>
          </div>

          <Button
            onClick={handleSignIn}
            className="w-full bg-[#1A1A1A] dark:bg-[#1A1A1A] hover:bg-[#262626] dark:hover:bg-[#262626] text-white dark:text-white border border-[rgba(255,255,255,0.12)] h-11 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <div className="flex items-center justify-center gap-3">
              <GoogleIcon />
              <span>{"Sign up with Google"}</span>
            </div>
          </Button>
        </div>
      </div>

      <div className="hidden w-1/2 lg:flex h-screen sticky top-0 bg-[#0B0F14]">
        <img
          src="/velocis-logo.webp"
          alt="Velocis Logo"
          className="w-full h-full object-cover pointer-events-none"
        />
      </div>
    </div>
  );
};

export default LoginPage;
