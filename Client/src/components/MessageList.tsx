import React, { useEffect, useRef } from 'react';
import { MessageItem } from './MessageItem';
import type { Message } from '../types';

interface MessageListProps {
    messages: Message[];
}

export const MessageList: React.FC<MessageListProps> = ({ messages }) => {
    const bottomRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, messages.length > 0 ? messages[messages.length - 1].content : null]);

    if (messages.length === 0) {
        return (
            <div className="flex-1 flex flex-col items-center justify-center text-zinc-500">
                <div className="w-16 h-16 bg-zinc-900 rounded-2xl flex items-center justify-center mb-4 border border-zinc-800">
                    <span className="text-2xl">✨</span>
                </div>
                <p className="text-lg font-medium text-zinc-300">How can I help you visualize data today?</p>
            </div>
        );
    }

    return (
        <div className="flex-1 overflow-y-auto custom-scrollbar pb-4">
            <div className="flex flex-col pb-4">
                {messages.map((msg) => (
                    <MessageItem key={msg.id} message={msg} />
                ))}
                <div ref={bottomRef} />
            </div>
        </div>
    );
};
