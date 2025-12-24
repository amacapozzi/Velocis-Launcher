import { useEffect, useState } from "react";

export type NavigationSection = "home" | "library" | "settings";

const getSectionFromPath = (path: string): NavigationSection => {
  if (path === "/" || path === "/home") {
    return "home";
  } else if (path === "/library") {
    return "library";
  } else if (path === "/settings") {
    return "settings";
  }
  return "home";
};

export const useNavigation = () => {
  const getCurrentSection = (): NavigationSection => {
    if (typeof window !== "undefined") {
      return getSectionFromPath(window.location.pathname);
    }
    return "home";
  };

  const [currentSection, setCurrentSection] =
    useState<NavigationSection>(getCurrentSection);

  useEffect(() => {
    const updateSection = () => {
      const section = getCurrentSection();
      setCurrentSection((prev) => {
        return section;
      });
    };

    updateSection();

    queueMicrotask(updateSection);

    const timeoutId = setTimeout(updateSection, 50);

    window.addEventListener("popstate", updateSection);

    window.addEventListener("focus", updateSection);

    return () => {
      clearTimeout(timeoutId);
      window.removeEventListener("popstate", updateSection);
      window.removeEventListener("focus", updateSection);
    };
  }, []);

  const navigateTo = (section: NavigationSection) => {
    const actualSection = getCurrentSection();
    if (actualSection === section) return;

    const path = section === "home" ? "/" : `/${section}`;

    if (typeof document !== "undefined" && "startViewTransition" in document) {
      try {
        // Start view transition - this will create a smooth cross-fade effect
        (document as any).startViewTransition(() => {
          window.location.href = path;
        });
      } catch (error) {
        window.location.href = path;
      }
    } else {
      window.location.href = path;
    }
  };

  return {
    currentSection,
    navigateTo,
  };
};
