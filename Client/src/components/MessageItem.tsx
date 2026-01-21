import React, { useState } from 'react';
import { User, Bot, Maximize2, X } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';
import { createPortal } from 'react-dom';
import type { Message } from '../types';

interface MessageItemProps {
    message: Message;
}

export const MessageItem: React.FC<MessageItemProps> = ({ message }) => {
    const isUser = message.role === 'user';
    const [isFullscreen, setIsFullscreen] = useState(false);

    const iframeSrcDoc = `
        <!DOCTYPE html>
        <html>
          <head>
            <style>
              body { margin: 0; padding: 20px; font-family: system-ui, -apple-system, sans-serif; color: #fff; background: transparent; }
              /* Scrollbar styling for inside iframe if needed */
              ::-webkit-scrollbar { width: 8px; height: 8px; }
              ::-webkit-scrollbar-track { background: #18181b; }
              ::-webkit-scrollbar-thumb { background: #3f3f46; border-radius: 4px; }
            </style>
          </head>
          <body>
            ${message.content}
            <script>
              window.onload = function() {
                  const height = document.body.scrollHeight;
                  window.parent.postMessage({ type: 'resize', height: height, id: '${message.id}' }, '*');
              }
            </script>
          </body>
        </html>
    `;

    return (
        <>
            <motion.div
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                className={`flex w-full gap-4 p-4 md:px-8 ${isUser ? 'justify-end' : 'justify-start bg-zinc-900/30'}`}
            >
                {!isUser && (
                    <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center shrink-0 border border-primary/20 mt-1">
                        <Bot size={18} className="text-primary" />
                    </div>
                )}

                <div className={`flex flex-col max-w-[85%] md:max-w-[75%] space-y-2 ${isUser ? 'items-end' : 'items-start'}`}>
                    <div className={`prose prose-invert max-w-none ${isUser ? 'bg-primary text-white px-4 py-2.5 rounded-2xl rounded-tr-sm' : 'w-full'}`}>
                        {message.isLoading ? (
                            <div className="flex space-x-2 py-2">
                                <div className="w-2 h-2 bg-zinc-500 rounded-full animate-bounce [animation-delay:-0.3s]"></div>
                                <div className="w-2 h-2 bg-zinc-500 rounded-full animate-bounce [animation-delay:-0.15s]"></div>
                                <div className="w-2 h-2 bg-zinc-500 rounded-full animate-bounce"></div>
                            </div>
                        ) : message.isHtml ? (
                            <div className="group relative w-full overflow-hidden rounded-xl bg-zinc-900 border border-zinc-800">
                                <div className="absolute top-2 right-2 z-30 opacity-0 group-hover:opacity-100 transition-opacity">
                                    <button
                                        type="button"
                                        onClick={(e) => {
                                            e.stopPropagation();
                                            setIsFullscreen(true);
                                        }}
                                        className="p-2 bg-zinc-800/90 hover:bg-zinc-700 text-zinc-100 rounded-xl border border-zinc-700 backdrop-blur-md shadow-lg cursor-pointer"
                                        title="Full Screen"
                                    >
                                        <Maximize2 size={18} />
                                    </button>
                                </div>
                                <iframe
                                    srcDoc={iframeSrcDoc}
                                    style={{ width: '100%', border: 'none', height: '500px' }}
                                    title="AI Content"
                                />
                            </div>
                        ) : (
                            <p className="whitespace-pre-wrap leading-relaxed">{message.content}</p>
                        )}
                    </div>
                </div>

                {isUser && (
                    <div className="w-8 h-8 rounded-full bg-zinc-800 flex items-center justify-center shrink-0 border border-zinc-700 mt-1">
                        <User size={18} className="text-zinc-400" />
                    </div>
                )}
            </motion.div>

            {typeof document !== 'undefined' && createPortal(
                <AnimatePresence>
                    {isFullscreen && (
                        <div className="fixed inset-0 z-[9999] flex items-center justify-center p-4 md:p-10">
                            {/* Backdrop */}
                            <motion.div
                                initial={{ opacity: 0 }}
                                animate={{ opacity: 1 }}
                                exit={{ opacity: 0 }}
                                onClick={() => setIsFullscreen(false)}
                                className="absolute inset-0 bg-black/90 backdrop-blur-md cursor-pointer"
                            />

                            {/* Modal Content */}
                            <motion.div
                                initial={{ opacity: 0, scale: 0.9, y: 20 }}
                                animate={{ opacity: 1, scale: 1, y: 0 }}
                                exit={{ opacity: 0, scale: 0.9, y: 20 }}
                                transition={{ type: "spring", damping: 25, stiffness: 300 }}
                                className="relative w-full h-full bg-zinc-950 rounded-2xl border border-zinc-800 shadow-[0_0_50px_-12px_rgba(0,0,0,0.5)] overflow-hidden flex flex-col z-[10000]"
                                onClick={(e) => e.stopPropagation()}
                            >
                                <div className="flex items-center justify-between px-6 py-4 border-b border-zinc-800 bg-zinc-900/50 backdrop-blur-xl">
                                    <div className="flex items-center gap-3">
                                        <div className="w-8 h-8 rounded-lg bg-primary/20 flex items-center justify-center border border-primary/20">
                                            <Bot size={18} className="text-primary" />
                                        </div>
                                        <div>
                                            <h3 className="text-zinc-100 font-semibold tracking-tight">AI Report Analysis</h3>
                                            <p className="text-[10px] text-zinc-500 uppercase tracking-widest font-bold">Full Screen Mode</p>
                                        </div>
                                    </div>
                                    <button
                                        onClick={() => setIsFullscreen(false)}
                                        className="p-2.5 hover:bg-zinc-800 text-zinc-400 hover:text-white rounded-xl transition-all duration-200 hover:scale-105 active:scale-95"
                                        title="Close"
                                    >
                                        <X size={22} />
                                    </button>
                                </div>
                                <div className="flex-1 w-full bg-zinc-950 p-1">
                                    <iframe
                                        srcDoc={iframeSrcDoc}
                                        className="w-full h-full border-none rounded-b-xl"
                                        title="AI Content Fullscreen"
                                    />
                                </div>
                            </motion.div>
                        </div>
                    )}
                </AnimatePresence>,
                document.body
            )}
        </>
    );
};


