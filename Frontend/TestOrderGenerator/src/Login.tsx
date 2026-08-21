import { useState } from 'react';
import axiosClient, { setToken, type TokenResponse } from './api/axiosClient';

interface LoginProps {
    onLoggedIn: () => void;
}

export default function Login({ onLoggedIn }: LoginProps) {
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [submitting, setSubmitting] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setSubmitting(true);
        setError('');
        try {
            const response = await axiosClient.post<TokenResponse>('/Auth/login', { username, password });
            setToken(response.data.token);
            onLoggedIn();
        } catch {
            setError('Sign-in failed. Check the feed integration credentials.');
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div className="page">
            <header className="header">
                <h1>Inbound Order Feed</h1>
                <p className="subtitle">Sign in as the upstream integration to push orders and receiving notices into the WMS.</p>
            </header>

            <section className="panel" style={{ maxWidth: 360 }}>
                <h2>Feed integration sign-in</h2>

                {error && <div className="error-banner">{error}</div>}

                <form onSubmit={(e) => void handleSubmit(e)}>
                    <div className="form-group">
                        <label htmlFor="username">Username</label>
                        <input
                            id="username"
                            type="text"
                            value={username}
                            onChange={(e) => setUsername(e.target.value)}
                            placeholder="erp-feed"
                        />
                    </div>
                    <div className="form-group">
                        <label htmlFor="password">Password</label>
                        <input
                            id="password"
                            type="password"
                            value={password}
                            onChange={(e) => setPassword(e.target.value)}
                        />
                    </div>
                    <button className="primary-btn" type="submit" disabled={submitting || !username || !password}>
                        {submitting ? 'Signing in...' : 'Sign in'}
                    </button>
                </form>
            </section>
        </div>
    );
}
