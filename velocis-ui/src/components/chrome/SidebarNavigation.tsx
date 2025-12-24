import { motion, AnimatePresence } from "framer-motion";
import { Home, Library, Settings } from "lucide-react";
import { useNavigation, type NavigationSection } from "@/hooks/use-navigation";
import { useState } from "react";

interface NavigationItem {
  key: NavigationSection;
  icon: typeof Home;
  label: string;
  tooltip: string;
}

const navigationItems: NavigationItem[] = [
  { key: "home", icon: Home, label: "Home", tooltip: "Home" },
  { key: "library", icon: Library, label: "Library", tooltip: "Library" },
  { key: "settings", icon: Settings, label: "Settings", tooltip: "Settings" },
];

interface TooltipProps {
  label: string;
  isVisible: boolean;
}

const Tooltip = ({ label, isVisible }: TooltipProps) => {
  return (
    <AnimatePresence>
      {isVisible && (
        <motion.div
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          exit={{ opacity: 0, x: -10 }}
          transition={{ duration: 0.18 }}
          className="absolute left-full ml-3 px-3 py-1.5 rounded-lg bg-black/80 backdrop-blur-sm text-white text-xs font-medium whitespace-nowrap pointer-events-none z-50 border border-white/10 shadow-[0_10px_30px_rgba(0,0,0,.45)]"
        >
          {label}
        </motion.div>
      )}
    </AnimatePresence>
  );
};

interface NavButtonProps {
  item: NavigationItem;
  isActive: boolean;
  onNavigate: (section: NavigationSection) => void;
}

const NavButton = ({ item, isActive, onNavigate }: NavButtonProps) => {
  const [showTooltip, setShowTooltip] = useState(false);
  const Icon = item.icon;

  return (
    <div
      className="relative"
      onMouseEnter={() => setShowTooltip(true)}
      onMouseLeave={() => setShowTooltip(false)}
    >
      <button
        type="button"
        aria-label={item.label}
        aria-current={isActive ? "page" : undefined}
        onClick={() => onNavigate(item.key)}
        className={[
          "relative w-11 h-11 rounded-full flex items-center justify-center",
          "transition-all duration-200 cursor-pointer",
          "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/20 focus-visible:ring-offset-0",
          isActive
            ? "bg-white/10 border border-white/12"
            : "bg-white/0 border border-white/0 hover:bg-white/6 hover:border-white/10",
        ].join(" ")}
      >
        <motion.div
          animate={{
            scale: isActive ? 1 : 0.9,
            opacity: isActive ? 1 : 0.7,
          }}
          transition={{ duration: 0.18 }}
        >
          <Icon
            className={
              isActive ? "w-5 h-5 text-white/90" : "w-5 h-5 text-white/55"
            }
          />
        </motion.div>

        {isActive && (
          <motion.div
            layoutId="activeIndicator"
            className="absolute inset-0 rounded-full bg-white/5 border border-white/10 pointer-events-none"
            transition={{ type: "spring", stiffness: 520, damping: 34 }}
          />
        )}
      </button>

      <Tooltip label={item.tooltip} isVisible={showTooltip} />
    </div>
  );
};

const SidebarNavigation = () => {
  const { currentSection, navigateTo } = useNavigation();

  return (
    <div
      className="fixed left-6 top-7 bottom-7 w-[64px] flex flex-col items-center z-[200] pointer-events-auto"
      style={{
        isolation: "isolate",
      }}
    >
      <motion.div
        initial={{ opacity: 0, x: -16 }}
        animate={{ opacity: 1, x: 0 }}
        transition={{ duration: 0.25 }}
        className="glass rounded-[20px] w-[64px] h-full px-2 py-3 flex flex-col items-center gap-3 relative pointer-events-auto"
      >
        {/* Logo / Top button */}
        <motion.button
          type="button"
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          className="w-11 h-11 rounded-full overflow-hidden focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/20"
          aria-label="Logo"
          onClick={() => navigateTo("home")}
        >
          <img
            src="/velocis-logo.webp"
            alt="Velocis Logo"
            className="w-full h-full object-cover"
          />
        </motion.button>

        <div className="mt-2 flex flex-col items-center gap-3">
          {navigationItems.map((item) => (
            <NavButton
              key={item.key}
              item={item}
              isActive={currentSection === item.key}
              onNavigate={navigateTo}
            />
          ))}
        </div>

        <div className="flex-1" />

        {/* Bottom button */}
        <motion.button
          type="button"
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          className="w-11 h-11 rounded-full hover:bg-white/6 border border-white/0 hover:border-white/10 transition focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/20"
          aria-label="Bottom icon"
        >
          <span className="w-2 h-2 rounded-full bg-white/25 inline-block" />
        </motion.button>
      </motion.div>
    </div>
  );
};

export default SidebarNavigation;
