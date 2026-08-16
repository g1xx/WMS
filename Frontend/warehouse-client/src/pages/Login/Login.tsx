import { useState } from 'react';
import axiosClient, { type TokenResponse } from '../../api/axiosClient';
import './Login.css';
import { useNavigate } from 'react-router-dom';

export default function Login() {
    const navigate = useNavigate();
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    
    const handleLogin = async (e: React.FormEvent) => {
        e.preventDefault(); 

        try {
            const response = await axiosClient.post<TokenResponse>('/Auth/login', {
                username: username,
                password: password
            });

            const token = response.data.token;

            localStorage.setItem('token', token);

            navigate('/tasks');

        } catch (error) {
            console.error("Login error:", error);
            alert("Login failed! Check your username and password.");
        }
    };

   return (
    <div className="login-container">
        <h2>WMS Sign In</h2>

        <form className="login-form" onSubmit={handleLogin}>
            <div className="form-group">
                <label>Username:</label>
                <input
                    type="text"
                    placeholder="Enter username"
                    value={username}
                    onChange={(e) => setUsername(e.target.value)}
                />
            </div>

            <div className="form-group">
                <label>Password:</label>
                <input
                    type="password"
                    placeholder="Enter password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
            </div>

            <button className="submit-btn" type="submit">Sign in to warehouse</button>
        </form>
    </div>
);
}