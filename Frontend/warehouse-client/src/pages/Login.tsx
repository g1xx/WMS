import { useState } from 'react';
import axiosClient from '../api/axiosClient';
import './Login.css';
import { useNavigate } from 'react-router-dom';

export default function Login() {
    const navigate = useNavigate();
    // 1. Создаем ячейки памяти для логина и пароля
    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    

    // 2. Эта функция сработает, когда кладовщик нажмет кнопку "Войти"
    const handleLogin = async (e: React.FormEvent) => {
        // Обязательная строчка: запрещаем браузеру перезагружать страницу при отправке формы
        e.preventDefault(); 

        try {
            const response = await axiosClient.post('/Auth/login', {
                username: username, // Ключ слева - это имя из LoginDto, значение справа - переменная из useState
                password: password
            });

            // 2. Достаем токен из ответа сервера (C# вернул { Token = ... }, Axios кладет это в response.data)
            const token = response.data.token;

            // 3. Кладем пропуск в память браузера
            localStorage.setItem('token', token);

            navigate('/tasks');

        } catch (error) {
            console.error("Ошибка при входе:", error);
            alert("Ошибка входа! Проверьте логин и пароль.");
        }
    };

   return (
    <div className="login-container">
        <h2>Вход в систему WMS</h2>
        
        <form className="login-form" onSubmit={handleLogin}>
            <div className="form-group">
                <label>Логин (Имя пользователя):</label>
                <input 
                    type="text" 
                    placeholder="Введите логин"
                    value={username} 
                    onChange={(e) => setUsername(e.target.value)} 
                />
            </div>

            <div className="form-group">
                <label>Пароль:</label>
                <input 
                    type="password" 
                    placeholder="Введите пароль"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                />
            </div>
            
            <button className="submit-btn" type="submit">Войти на склад</button>
        </form>
    </div>
);
}