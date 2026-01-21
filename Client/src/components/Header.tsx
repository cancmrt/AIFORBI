import React from 'react';
import { Menu, Sparkles } from 'lucide-react';

export const Header: React.FC = () => {
    return (
        <header className="sticky top-0 z-10 flex items-center justify-between px-4 py-3 bg-background/80 backdrop-blur-md border-b border-zinc-800/50 md:hidden">
            <button className="p-2 -ml-2 text-zinc-400 hover:text-white transition-colors rounded-lg hover:bg-zinc-800">
                <Menu size={20} />
            </button>
            <div className="flex items-center gap-2 font-semibold text-zinc-100">
                <Sparkles className="text-primary w-5 h-5" />
                <span>AI SQL Analyst</span>
            </div>
            <div className="w-8" /> {/* Spacer for centering */}
        </header>
    );
};
