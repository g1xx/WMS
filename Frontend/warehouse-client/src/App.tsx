import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login/Login';
import PickTasks from './pages/PickTasks/PickTasks';
import InventoryAdmin from './pages/Admin/InventoryAdmin';
import type { JSX } from 'react/jsx-runtime';

const ProtectedRoute = ({ children }: { children: JSX.Element }) => {
    const token = localStorage.getItem('token');
    
    if (!token) {
        return <Navigate to="/login" replace />;
    }
    
    return children;
};

export default function App() {
    return (
        <BrowserRouter>
            <Routes>
                <Route path="/login" element={<Login />} />

                <Route
                    path="/"
                    element={
                        <ProtectedRoute>
                            <PickTasks />
                        </ProtectedRoute>
                    }
                />

                <Route
                    path="/admin/inventory"
                    element={
                        <ProtectedRoute>
                            <InventoryAdmin />
                        </ProtectedRoute>
                    }
                />

                <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
        </BrowserRouter>
    );
}