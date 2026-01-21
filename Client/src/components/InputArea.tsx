import React, { useState, useRef, useEffect } from 'react';
import { SendHorizontal } from 'lucide-react';

interface InputAreaProps {
    onSend: (message: string) => void;
    disabled?: boolean;
}

export const InputArea: React.FC<InputAreaProps> = ({ onSend, disabled }) => {
    const [input, setInput] = useState('');
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    const handleSubmit = (e?: React.FormEvent) => {
        e?.preventDefault();
        if (!input.trim() || disabled) return;
        onSend(input);
        setInput('');
        if (textareaRef.current) textareaRef.current.style.height = 'auto';
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSubmit();
        }
    };

    const adjustHeight = () => {
        if (textareaRef.current) {
            textareaRef.current.style.height = 'auto';
            textareaRef.current.style.height = `${Math.min(textareaRef.current.scrollHeight, 200)}px`;
        }
    };

    useEffect(() => {
        adjustHeight();
    }, [input]);

    return (
        <div className="w-full max-w-4xl mx-auto px-4 pb-6 pt-2">
            <div className="relative flex items-end gap-2 bg-zinc-900/50 p-2 rounded-xl border border-zinc-700/50 focus-within:ring-2 focus-within:ring-primary/40 focus-within:border-primary/40 transition-all shadow-lg backdrop-blur-sm">


                <textarea
                    ref={textareaRef}
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={handleKeyDown}
                    placeholder="Ask a question about your data..."
                    className="flex-1 max-h-[200px] min-h-[24px] bg-transparent border-0 focus:ring-0 resize-none py-3 text-zinc-100 placeholder-zinc-500 overflow-y-auto scrollbar-hide"
                    rows={1}
                    disabled={disabled}
                />

                <button
                    onClick={() => handleSubmit()}
                    disabled={!input.trim() || disabled}
                    className={`p-2 rounded-lg mb-1 transition-all duration-200 ${input.trim() && !disabled
                        ? 'bg-primary text-white shadow-md hover:bg-primary/90'
                        : 'bg-zinc-800 text-zinc-500 cursor-not-allowed'
                        }`}
                >
                    <SendHorizontal size={20} />
                </button>
            </div>
            <div className="text-center mt-2">
                <p className="text-xs text-zinc-500">AI can make mistakes. Verify important information.</p>
            </div>
        </div>
    );
};
