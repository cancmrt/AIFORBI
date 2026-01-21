import React, { useState } from 'react';
import { User, Lock, LogIn, Sparkles } from 'lucide-react';
import { motion } from 'framer-motion';

interface LoginPageProps {
    onLogin: (userId: number, email: string, displayName: string) => void;
}

export const LoginPage: React.FC<LoginPageProps> = ({ onLogin }) => {
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');
        setIsLoading(true);

        try {
            const response = await fetch('/api/Auth/Login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, password }),
            });

            if (!response.ok) {
                const errorText = await response.text();
                throw new Error(errorText || 'Login failed');
            }

            const data = await response.json();
            onLogin(data.userId, data.email, data.displayName);
        } catch (err: any) {
            setError(err.message || 'Invalid email or password');
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <div className="min-h-screen bg-background flex items-center justify-center p-4">
            <motion.div
                initial={{ opacity: 0, y: 20 }}
                animate={{ opacity: 1, y: 0 }}
                className="w-full max-w-md"
            >
                {/* Logo/Header */}
                <div className="text-center mb-8">
                    <div className="inline-flex items-center justify-center w-16 h-16 bg-primary/20 rounded-2xl mb-4 border border-primary/30">
                        <Sparkles size={32} className="text-primary" />
                    </div>
                    <h1 className="text-3xl font-bold text-zinc-100 mb-2">AIFORBI</h1>
                    <p className="text-zinc-500">AI-Powered Business Intelligence</p>
                </div>

                {/* Login Card */}
                <div className="bg-zinc-900/50 backdrop-blur-xl border border-zinc-800 rounded-2xl p-8 shadow-2xl">
                    <h2 className="text-xl font-semibold text-zinc-100 mb-6 text-center">Sign In</h2>

                    <form onSubmit={handleSubmit} className="space-y-5">
                        {/* Email Field */}
                        <div>
                            <label className="block text-sm font-medium text-zinc-400 mb-2">
                                Email
                            </label>
                            <div className="relative">
                                <User size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500" />
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    placeholder="admin@admin.com"
                                    className="w-full pl-10 pr-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary/50 transition-all"
                                    required
                                />
                            </div>
                        </div>

                        {/* Password Field */}
                        <div>
                            <label className="block text-sm font-medium text-zinc-400 mb-2">
                                Password
                            </label>
                            <div className="relative">
                                <Lock size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-zinc-500" />
                                <input
                                    type="password"
                                    value={password}
                                    onChange={(e) => setPassword(e.target.value)}
                                    placeholder="••••••"
                                    className="w-full pl-10 pr-4 py-3 bg-zinc-800/50 border border-zinc-700 rounded-xl text-zinc-100 placeholder-zinc-500 focus:outline-none focus:ring-2 focus:ring-primary/50 focus:border-primary/50 transition-all"
                                    required
                                />
                            </div>
                        </div>

                        {/* Error Message */}
                        {error && (
                            <motion.div
                                initial={{ opacity: 0, y: -10 }}
                                animate={{ opacity: 1, y: 0 }}
                                className="p-3 bg-red-500/10 border border-red-500/30 rounded-xl text-red-400 text-sm text-center"
                            >
                                {error}
                            </motion.div>
                        )}

                        {/* Submit Button */}
                        <button
                            type="submit"
                            disabled={isLoading}
                            className="w-full py-3 px-4 bg-primary hover:bg-primary/90 disabled:bg-primary/50 text-white font-medium rounded-xl transition-all duration-200 flex items-center justify-center gap-2 shadow-lg shadow-primary/20"
                        >
                            {isLoading ? (
                                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                            ) : (
                                <>
                                    <LogIn size={18} />
                                    Sign In
                                </>
                            )}
                        </button>
                    </form>

                    {/* Demo Hint */}
                    <div className="mt-6 pt-6 border-t border-zinc-800">
                        <p className="text-xs text-zinc-500 text-center">
                            Demo: <span className="text-zinc-400">admin@admin.com</span> / <span className="text-zinc-400">123456</span>
                        </p>
                    </div>
                </div>
            </motion.div>
        </div>
    );
};
