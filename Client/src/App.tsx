import React, { useState, useEffect } from 'react';
import { ChatLayout } from './components/ChatLayout';
import { Header } from './components/Header';
import { MessageList } from './components/MessageList';
import { InputArea } from './components/InputArea';
import { LoginPage } from './components/LoginPage';
import { SettingsPage } from './components/SettingsPage';
import type { Message, User } from './types';

interface ChatSession {
  id: number;
  sessionId: string;
  title: string | null;
  createdAt: string;
}

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [sessions, setSessions] = useState<ChatSession[]>([]);
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showSettings, setShowSettings] = useState(false);
  const [isConfigured, setIsConfigured] = useState(true);

  // Check for existing user on mount
  useEffect(() => {
    const storedUser = localStorage.getItem('aiforbi_user');
    if (storedUser) {
      const userData = JSON.parse(storedUser);
      setUser(userData);
    }
  }, []);

  // Check if app is configured on user login
  useEffect(() => {
    if (user) {
      checkConfiguration();
      fetchSessions();
    }
  }, [user]);

  const checkConfiguration = async () => {
    try {
      const res = await fetch('/api/Settings/IsConfigured');
      if (res.ok) {
        const data = await res.json();
        setIsConfigured(data.isConfigured);
        if (!data.isConfigured && user?.role === 'admin') {
          setShowSettings(true);
        }
      }
    } catch (err) {
      console.error('Failed to check configuration', err);
    }
  };

  // Fetch history when session changes
  useEffect(() => {
    if (currentSessionId) {
      fetchHistory(currentSessionId);
    } else {
      setMessages([]);
    }
  }, [currentSessionId]);

  const fetchSessions = async () => {
    if (!user) return;
    try {
      const res = await fetch(`/api/Report/Sessions?userId=${user.userId}`);
      if (res.ok) {
        const data = await res.json();
        setSessions(data);
        // Auto-select most recent session if none selected
        if (data.length > 0 && !currentSessionId) {
          setCurrentSessionId(data[0].sessionId);
        }
      }
    } catch (err) {
      console.error('Failed to load sessions', err);
    }
  };

  const fetchHistory = async (sessionId: string) => {
    setIsLoading(true);
    try {
      const res = await fetch(`/api/Report/History?sessionId=${sessionId}`);
      if (res.ok) {
        const history = await res.json();
        const mapped: Message[] = history.map((h: any) => ({
          id: h.id.toString(),
          role: h.role,
          content: h.content,
          isHtml: h.isHtml
        }));
        setMessages(mapped);
      }
    } catch (err) {
      console.error('Failed to load history', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogin = (userId: number, email: string, displayName: string, role: string) => {
    const userData = { userId, email, displayName, role: role as 'admin' | 'user' };
    setUser(userData);
    localStorage.setItem('aiforbi_user', JSON.stringify(userData));
  };

  const handleLogout = () => {
    setUser(null);
    setSessions([]);
    setCurrentSessionId(null);
    setMessages([]);
    localStorage.removeItem('aiforbi_user');
  };

  const handleNewChat = async () => {
    if (!user) return;
    try {
      const res = await fetch('/api/Report/Sessions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userId: user.userId, title: null }),
      });
      if (res.ok) {
        const newSession = await res.json();
        setSessions(prev => [newSession, ...prev]);
        setCurrentSessionId(newSession.sessionId);
        setMessages([]);
      }
    } catch (err) {
      console.error('Failed to create session', err);
    }
  };

  const handleSelectSession = (sessionId: string) => {
    setCurrentSessionId(sessionId);
  };

  const handleSendMessage = async (text: string) => {
    if (!user) return;

    // If no session, create one first
    let sessionId = currentSessionId;
    if (!sessionId) {
      try {
        const res = await fetch('/api/Report/Sessions', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ userId: user.userId, title: text.slice(0, 50) }),
        });
        if (res.ok) {
          const newSession = await res.json();
          setSessions(prev => [newSession, ...prev]);
          sessionId = newSession.sessionId;
          setCurrentSessionId(sessionId);
        }
      } catch (err) {
        console.error('Failed to create session', err);
        return;
      }
    }

    // Add user message
    const userMsg: Message = {
      id: Date.now().toString(),
      role: 'user',
      content: text,
    };
    setMessages(prev => [...prev, userMsg]);
    setIsLoading(true);

    const assistantMsgId = (Date.now() + 1).toString();

    try {
      const response = await fetch('/api/Report/Ask', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          Question: text,
          DrawGraphic: true,
          SessionId: sessionId
        }),
      });

      if (!response.ok) {
        throw new Error(`API Error: ${response.status}`);
      }

      const rawData = await response.text();
      let content = rawData;

      // Parse JSON response
      try {
        const parsed = JSON.parse(rawData);
        if (typeof parsed === 'object' && parsed !== null) {
          const htmlSource = parsed.generatedGraphicHtmlCode || parsed.GeneratedGraphicHtmlCode;
          const textSource = parsed.answer || parsed.Answer || parsed.content || parsed.Content;

          if (htmlSource) {
            content = htmlSource;
          } else if (textSource) {
            content = textSource;
          }
        } else if (typeof parsed === 'string') {
          content = parsed;
        }
      } catch (e) {
        content = rawData;
      }

      const isResponseHtml = /<[a-z][\s\S]*>/i.test(content) || content.includes('<!DOCTYPE');

      const assistantMsg: Message = {
        id: assistantMsgId,
        role: 'assistant',
        content: content,
        isHtml: isResponseHtml,
        isLoading: false
      };

      setMessages(prev => [...prev, assistantMsg]);

      // Update session title with first message if untitled
      const currentSession = sessions.find(s => s.sessionId === sessionId);
      if (currentSession && !currentSession.title) {
        try {
          await fetch(`/api/Report/Sessions/${sessionId}/Title`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ title: text.slice(0, 50) }),
          });
          setSessions(prev => prev.map(s =>
            s.sessionId === sessionId ? { ...s, title: text.slice(0, 50) } : s
          ));
        } catch (e) { }
      }

    } catch (error) {
      console.error(error);
      const errorMsg: Message = {
        id: assistantMsgId,
        role: 'assistant',
        content: 'Sorry, I encountered an error while processing your request. Please try again.',
        isHtml: false,
        isLoading: false
      };
      setMessages(prev => [...prev, errorMsg]);
    } finally {
      setIsLoading(false);
    }
  };

  // Show login page if not authenticated
  if (!user) {
    return <LoginPage onLogin={handleLogin} />;
  }

  // Show settings page if user is viewing settings or app is not configured for admin
  if (showSettings && user.role === 'admin') {
    return (
      <div>
        <div className="p-4 bg-zinc-900 border-b border-zinc-800 flex justify-between items-center">
          <h1 className="text-lg font-semibold text-zinc-100">AIFORBI</h1>
          <button
            onClick={() => setShowSettings(false)}
            className="px-4 py-2 bg-zinc-800 hover:bg-zinc-700 text-zinc-100 rounded-lg transition"
          >
            Back to Chat
          </button>
        </div>
        <SettingsPage user={user} />
      </div>
    );
  }

  return (
    <ChatLayout
      sessions={sessions}
      currentSessionId={currentSessionId}
      onSelectSession={handleSelectSession}
      onNewChat={handleNewChat}
      onLogout={handleLogout}
      userDisplayName={user.displayName}
      userRole={user.role}
      onShowSettings={() => setShowSettings(true)}
    >
      <Header />
      <div className="flex-1 flex flex-col relative overflow-hidden">
        <MessageList messages={messages} />
        {isLoading && messages.length > 0 && messages[messages.length - 1].role === 'user' && (
          <div className="px-4 md:px-8 py-2">
            <span className="text-sm text-zinc-500 animate-pulse">Thinking...</span>
          </div>
        )}
        <InputArea onSend={handleSendMessage} disabled={isLoading} />
      </div>
    </ChatLayout>
  );
}

export default App;
