import React, { useState, useEffect } from 'react';
import { Save, Play, AlertCircle, CheckCircle } from 'lucide-react';
import { motion } from 'framer-motion';
import type { User } from '../types';

interface SettingsPageProps {
    user: User;
}

interface Settings {
    connStrs: {
        dbConnector: {
            type: string;
            mssql: {
                connStr: string;
                databaseName: string;
                schema: string;
            };
        };
        ollama: {
            baseUrl: string;
            chatModel: string;
            embedModel: string;
        };
        qdrant: {
            host: string;
            grpc: string;
        };
        ai: {
            chatProvider: string;
            embedProvider: string;
        };
        gemini: {
            apiKey: string;
            model: string;
            fallbackModels: string[];
        };
    };
    logging: {
        logLevel: {
            default: string;
            microsoftAspNetCore: string;
        };
    };
    allowedHosts: string;
}

export const SettingsPage: React.FC<SettingsPageProps> = ({ user }) => {
    const [settings, setSettings] = useState<Settings | null>(null);
    const [isLoading, setIsLoading] = useState(false);
    const [isSaving, setIsSaving] = useState(false);
    const [isRunningIndex, setIsRunningIndex] = useState(false);
    const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

    // Redirect non-admins
    useEffect(() => {
        if (user.role !== 'admin') {
            window.location.href = '/';
        }
    }, [user]);

    // Fetch settings on mount
    useEffect(() => {
        fetchSettings();
    }, []);

    const fetchSettings = async () => {
        setIsLoading(true);
        try {
            const res = await fetch('/api/Settings/GetSettings');
            if (res.ok) {
                const data = await res.json();
                setSettings(data);
            } else {
                setMessage({ type: 'error', text: 'Failed to load settings' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'Error loading settings' });
        } finally {
            setIsLoading(false);
        }
    };

    const handleSave = async () => {
        if (!settings) return;

        setIsSaving(true);
        setMessage(null);
        try {
            const res = await fetch('/api/Settings/UpdateSettings', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(settings),
            });

            if (res.ok) {
                setMessage({ type: 'success', text: 'Settings saved successfully!' });
            } else {
                setMessage({ type: 'error', text: 'Failed to save settings' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'Error saving settings' });
        } finally {
            setIsSaving(false);
        }
    };

    const handleRunIndexing = async () => {
        setIsRunningIndex(true);
        setMessage(null);
        try {
            const res = await fetch('/api/Settings/GetDbSummary?UseForceAI=true');
            if (res.ok) {
                setMessage({ type: 'success', text: 'DB Indexing completed successfully!' });
            } else {
                setMessage({ type: 'error', text: 'Failed to run indexing' });
            }
        } catch (err) {
            setMessage({ type: 'error', text: 'Error running indexing' });
        } finally {
            setIsRunningIndex(false);
        }
    };

    if (user.role !== 'admin') {
        return null; // Will redirect
    }

    if (isLoading || !settings) {
        return (
            <div className="min-h-screen bg-background flex items-center justify-center">
                <div className="text-zinc-400">Loading settings...</div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-background p-6">
            <div className="max-w-4xl mx-auto">
                <motion.div
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                >
                    <h1 className="text-3xl font-bold text-zinc-100 mb-6">Settings</h1>

                    {/* Message */}
                    {message && (
                        <motion.div
                            initial={{ opacity: 0, y: -10 }}
                            animate={{ opacity: 1, y: 0 }}
                            className={`p-4 rounded-xl mb-6 flex items-center gap-2 ${message.type === 'success'
                                ? 'bg-green-500/10 border border-green-500/30 text-green-400'
                                : 'bg-red-500/10 border border-red-500/30 text-red-400'
                                }`}
                        >
                            {message.type === 'success' ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
                            {message.text}
                        </motion.div>
                    )}

                    {/* Database Settings */}
                    <div className="bg-zinc-900/50 backdrop-blur-xl border border-zinc-800 rounded-2xl p-6 mb-6">
                        <h2 className="text-xl font-semibold text-zinc-100 mb-4">Database Configuration</h2>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Connection String</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.dbConnector.mssql.connStr}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            dbConnector: {
                                                ...settings.connStrs.dbConnector,
                                                mssql: { ...settings.connStrs.dbConnector.mssql, connStr: e.target.value }
                                            }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Database Name</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.dbConnector.mssql.databaseName}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            dbConnector: {
                                                ...settings.connStrs.dbConnector,
                                                mssql: { ...settings.connStrs.dbConnector.mssql, databaseName: e.target.value }
                                            }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Schema</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.dbConnector.mssql.schema}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            dbConnector: {
                                                ...settings.connStrs.dbConnector,
                                                mssql: { ...settings.connStrs.dbConnector.mssql, schema: e.target.value }
                                            }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                        </div>
                    </div>

                    {/* Ollama Settings */}
                    <div className="bg-zinc-900/50 backdrop-blur-xl border border-zinc-800 rounded-2xl p-6 mb-6">
                        <h2 className="text-xl font-semibold text-zinc-100 mb-4">Ollama Configuration</h2>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Base URL</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.ollama.baseUrl}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            ollama: { ...settings.connStrs.ollama, baseUrl: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Chat Model</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.ollama.chatModel}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            ollama: { ...settings.connStrs.ollama, chatModel: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Embed Model</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.ollama.embedModel}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            ollama: { ...settings.connStrs.ollama, embedModel: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                        </div>
                    </div>

                    {/* AI Provider Settings */}
                    <div className="bg-zinc-900/50 backdrop-blur-xl border border-zinc-800 rounded-2xl p-6 mb-6">
                        <h2 className="text-xl font-semibold text-zinc-100 mb-4">AI Provider Configuration</h2>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Chat Provider</label>
                                <select
                                    value={settings.connStrs.ai.chatProvider}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            ai: { ...settings.connStrs.ai, chatProvider: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                >
                                    <option value="Ollama">Ollama</option>
                                    <option value="Gemini">Gemini</option>
                                </select>
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Embed Provider</label>
                                <select
                                    value={settings.connStrs.ai.embedProvider}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            ai: { ...settings.connStrs.ai, embedProvider: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                >
                                    <option value="Ollama">Ollama</option>
                                    <option value="Gemini">Gemini (Not Implemented)</option>
                                </select>
                            </div>
                        </div>
                    </div>

                    {/* Gemini Settings */}
                    <div className="bg-zinc-900/50 backdrop-blur-xl border border-zinc-800 rounded-2xl p-6 mb-6">
                        <h2 className="text-xl font-semibold text-zinc-100 mb-4">Gemini Configuration</h2>
                        <div className="space-y-4">
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">API Key</label>
                                <input
                                    type="password"
                                    value={settings.connStrs.gemini.apiKey}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            gemini: { ...settings.connStrs.gemini, apiKey: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">Primary Model</label>
                                <input
                                    type="text"
                                    value={settings.connStrs.gemini.model}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            gemini: { ...settings.connStrs.gemini, model: e.target.value }
                                        }
                                    })}
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                            <div>
                                <label className="block text-sm font-medium text-zinc-400 mb-2">
                                    Fallback Models <span className="text-xs text-zinc-500">(comma-separated)</span>
                                </label>
                                <input
                                    type="text"
                                    value={settings.connStrs.gemini.fallbackModels?.join(', ') || ''}
                                    onChange={(e) => setSettings({
                                        ...settings,
                                        connStrs: {
                                            ...settings.connStrs,
                                            gemini: {
                                                ...settings.connStrs.gemini,
                                                fallbackModels: e.target.value.split(',').map(m => m.trim()).filter(m => m)
                                            }
                                        }
                                    })}
                                    placeholder="gemini-2.5-flash, gemini-2.0-flash"
                                    className="w-full px-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50"
                                />
                            </div>
                        </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex gap-4">
                        <button
                            onClick={handleSave}
                            disabled={isSaving}
                            className="flex-1 py-3 px-6 bg-primary hover:bg-primary/90 disabled:bg-primary/50 text-white font-medium rounded-xl transition-all flex items-center justify-center gap-2"
                        >
                            {isSaving ? (
                                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                            ) : (
                                <>
                                    <Save size={18} />
                                    Save Settings
                                </>
                            )}
                        </button>
                        <button
                            onClick={handleRunIndexing}
                            disabled={isRunningIndex}
                            className="flex-1 py-3 px-6 bg-green-600 hover:bg-green-700 disabled:bg-green-600/50 text-white font-medium rounded-xl transition-all flex items-center justify-center gap-2"
                        >
                            {isRunningIndex ? (
                                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                            ) : (
                                <>
                                    <Play size={18} />
                                    Run DB Indexing
                                </>
                            )}
                        </button>
                    </div>
                </motion.div>
            </div>
        </div>
    );
};
