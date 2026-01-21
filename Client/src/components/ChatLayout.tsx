import React from 'react';
import { Plus, MessageSquare, LogOut } from 'lucide-react';

interface ChatSession {
    id: number;
    sessionId: string;
    title: string | null;
    createdAt: string;
}

interface ChatLayoutProps {
    children: React.ReactNode;
    sessions: ChatSession[];
    currentSessionId: string | null;
    onSelectSession: (sessionId: string) => void;
    onNewChat: () => void;
    onLogout: () => void;
    userDisplayName: string;
}

export const ChatLayout: React.FC<ChatLayoutProps> = ({
    children,
    sessions,
    currentSessionId,
    onSelectSession,
    onNewChat,
    onLogout,
    userDisplayName
}) => {
    return (
        <div className="flex h-screen bg-background text-zinc-100 overflow-hidden font-sans">
            {/* Sidebar - Hidden on mobile, fixed width on desktop */}
            <aside className="hidden md:flex flex-col w-[260px] bg-black border-r border-zinc-800 p-3 gap-2">
                <button
                    onClick={onNewChat}
                    className="flex items-center gap-2 w-full px-3 py-3 rounded-lg border border-zinc-700 bg-zinc-900 hover:bg-zinc-800 transition-colors text-sm text-zinc-200 font-medium"
                >
                    <Plus size={16} />
                    <span>New Chat</span>
                </button>

                <div className="flex-1 overflow-y-auto py-2 space-y-1">
                    <div className="px-3 py-2 text-xs font-medium text-zinc-500 uppercase tracking-wider">
                        History
                    </div>
                    {sessions.length === 0 ? (
                        <p className="px-3 py-2 text-xs text-zinc-600">No chats yet</p>
                    ) : (
                        sessions.map((session) => (
                            <button
                                key={session.id}
                                onClick={() => onSelectSession(session.sessionId)}
                                className={`flex items-center gap-3 w-full px-3 py-3 rounded-lg transition-colors text-sm text-left truncate ${currentSessionId === session.sessionId
                                        ? 'bg-zinc-800 text-zinc-100'
                                        : 'hover:bg-zinc-900 text-zinc-400 hover:text-zinc-200'
                                    }`}
                            >
                                <MessageSquare size={16} className="shrink-0" />
                                <span className="truncate">
                                    {session.title || `Chat ${new Date(session.createdAt).toLocaleDateString()}`}
                                </span>
                            </button>
                        ))
                    )}
                </div>

                <div className="mt-auto px-3 py-3 border-t border-zinc-800">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-primary to-purple-500 flex items-center justify-center text-white text-sm font-bold">
                                {userDisplayName?.charAt(0)?.toUpperCase() || 'U'}
                            </div>
                            <div className="text-sm font-medium truncate max-w-[120px]">{userDisplayName}</div>
                        </div>
                        <button
                            onClick={onLogout}
                            className="p-2 hover:bg-zinc-800 text-zinc-500 hover:text-zinc-200 rounded-lg transition-colors"
                            title="Logout"
                        >
                            <LogOut size={16} />
                        </button>
                    </div>
                </div>
            </aside>

            {/* Main Content */}
            <main className="flex-1 flex flex-col relative w-full h-full max-w-full bg-background">
                {children}
            </main>
        </div>
    );
};
