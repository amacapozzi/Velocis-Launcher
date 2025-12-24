import { motion } from "framer-motion";

const game = {
  label: "Most Played",
  title: "STRANGE\nCLAN",
  desc: "Strange Clan™ is an online, action RPG, using\nPlay2-Own mechanics to give players\nownership of their digital items in game.",
};

const containerVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      staggerChildren: 0.15,
      delayChildren: 0.2,
    },
  },
};

const itemVariants = {
  hidden: { opacity: 0, y: 20 },
  visible: {
    opacity: 1,
    y: 0,
    transition: {
      duration: 0.6,
      ease: [0.22, 1, 0.36, 1] as any,
    },
  },
};

const fadeInVariants = {
  hidden: { opacity: 0 },
  visible: {
    opacity: 1,
    transition: {
      duration: 0.8,
      ease: [0.22, 1, 0.36, 1],
    },
  },
};

export const HeroStage = () => {
  return (
    <motion.div
      className="absolute inset-0"
      initial="hidden"
      animate="visible"
      variants={containerVariants}
    >
      {/* mask */}
      <div className="absolute inset-0 rounded-xl overflow-hidden pointer-events-none">
        <div className="hero-mask" />
      </div>

      {/* LABEL */}
      <motion.div
        className="absolute top-20"
        style={{ left: "calc(var(--left-rail-w) + var(--stage-gap))" }}
        variants={itemVariants}
      >
        <motion.div
          className="glass-soft rounded-full px-4 h-[32px] flex items-center text-[11px] tracking-[0.2em] uppercase text-white/80 font-bold"
          whileHover={{ scale: 1.05 }}
        >
          <motion.span
            className="inline-block w-2 h-2 rounded-full bg-gradient-to-r from-blue-400 to-purple-500 mr-2"
            animate={{
              scale: [1, 1.2, 1],
              opacity: [0.8, 1, 0.8],
            }}
            transition={{
              duration: 2,
              repeat: Infinity,
              ease: "easeInOut",
            }}
          />
          {game.label}
        </motion.div>
      </motion.div>

      {/* MAIN CONTENT */}
      <motion.div
        className="absolute top-1/2 -translate-y-[50%] w-[600px]"
        style={{ left: "calc(var(--left-rail-w) + var(--stage-gap))" }}
        variants={itemVariants}
      >
        <motion.div
          className="flex items-start gap-6 mb-8"
          variants={containerVariants}
        >
          <motion.div
            className="w-[72px] h-[72px] rounded-2xl bg-gradient-to-br from-purple-500/30 via-blue-500/20 to-cyan-500/20 border border-white/20 shadow-[0_16px_40px_rgba(139,92,246,0.3)] backdrop-blur-md flex-shrink-0"
            variants={itemVariants}
            whileHover={{
              scale: 1.1,
              rotate: 5,
              boxShadow: "0_20px_50px_rgba(139,92,246,0.4)",
            }}
            animate={{
              boxShadow: [
                "0_16px_40px_rgba(139,92,246,0.3)",
                "0_20px_50px_rgba(139,92,246,0.4)",
                "0_16px_40px_rgba(139,92,246,0.3)",
              ],
            }}
            transition={{
              duration: 3,
              repeat: Infinity,
              ease: "easeInOut",
            }}
          />

          <motion.div className="flex-1" variants={itemVariants}>
            <motion.h1
              className="text-[80px] leading-[88px] font-black tracking-[-0.02em] text-white whitespace-pre-line mb-4"
              variants={itemVariants}
            >
              <motion.span
                className="block bg-gradient-to-r from-white via-white/95 to-white/90 bg-clip-text text-transparent"
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.8, delay: 0.3 }}
              >
                {game.title}
              </motion.span>
            </motion.h1>
          </motion.div>
        </motion.div>

        <motion.div
          className="max-w-[520px] text-[16px] leading-[24px] text-white/70 whitespace-pre-line mb-10"
          variants={itemVariants}
        >
          <motion.p
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.8, delay: 0.5 }}
          >
            {game.desc}
          </motion.p>
        </motion.div>

        <motion.div
          className="flex items-center gap-4"
          variants={containerVariants}
        >
          <motion.button
            className="h-[44px] px-8 rounded-full bg-white text-black text-[14px] font-bold shadow-[0_16px_40px_rgba(0,0,0,.6)] hover:shadow-[0_20px_50px_rgba(0,0,0,.7)] transition-all duration-300"
            variants={itemVariants}
            whileHover={{
              scale: 1.05,
              y: -2,
            }}
            whileTap={{ scale: 0.98 }}
          >
            PLAY NOW
          </motion.button>

          <motion.button
            className="h-[44px] px-6 rounded-full glass-soft text-[13px] font-semibold text-white/90 flex items-center gap-2.5 hover:text-white hover:bg-white/10 transition-all duration-300 border border-white/20"
            variants={itemVariants}
            whileHover={{
              scale: 1.05,
              y: -2,
            }}
            whileTap={{ scale: 0.98 }}
          >
            <motion.span
              className="w-3.5 h-3.5 rounded-full bg-gradient-to-r from-blue-400 to-purple-500"
              animate={{
                scale: [1, 1.2, 1],
              }}
              transition={{
                duration: 2,
                repeat: Infinity,
                ease: "easeInOut",
              }}
            />
            ADD TO FAVORITE
          </motion.button>
        </motion.div>
      </motion.div>

      {/* DOWN ARROW */}
      <motion.div className="absolute left-1/2 bottom-5 -translate-x-1/2">
        <motion.button
          className="w-9 h-9 rounded-full glass-soft flex items-center justify-center hover:bg-white/10 transition"
          aria-label="Down"
          whileHover={{ scale: 1.1, y: 2 }}
          whileTap={{ scale: 0.95 }}
        >
          <div className="w-2.5 h-2.5 border-b border-r border-white/55 rotate-45 -translate-y-px" />
        </motion.button>
      </motion.div>
    </motion.div>
  );
};
