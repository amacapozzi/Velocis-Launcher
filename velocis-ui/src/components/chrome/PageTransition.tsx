import { motion, AnimatePresence } from "framer-motion";
import { useEffect, useState } from "react";
import type { NavigationSection } from "@/hooks/use-navigation";

interface PageTransitionProps {
  children: React.ReactNode;
  section: NavigationSection;
}

export const PageTransition = ({ children, section }: PageTransitionProps) => {
  const [displaySection, setDisplaySection] = useState<NavigationSection>(section);

  useEffect(() => {
    const handleNavigation = (event: CustomEvent<{ section: NavigationSection }>) => {
      setDisplaySection(event.detail.section);
    };

    window.addEventListener("navigation" as any, handleNavigation as EventListener);
    return () => {
      window.removeEventListener("navigation" as any, handleNavigation as EventListener);
    };
  }, []);

  return (
    <AnimatePresence mode="wait">
      <motion.div
        key={displaySection}
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        exit={{ opacity: 0, y: -20 }}
        transition={{
          duration: 0.3,
          ease: [0.22, 1, 0.36, 1],
        }}
        className="w-full h-full"
      >
        {children}
      </motion.div>
    </AnimatePresence>
  );
};

