import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import Login from './pages/Login/Login';
import Terminal from './pages/Terminal/Terminal';
import InventoryAdmin from './pages/Admin/InventoryAdmin';
import HelpPanel from './components/HelpPanel';
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
                            <Terminal />
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

            {/* Outside <Routes> on purpose: one mount covers every screen, login included —
                which is the screen that needs it most, since a reviewer has no credentials
                until this panel gives them some. Renders nothing when the demo endpoint is
                disabled, so it costs a real deployment a single 404 and no UI. */}
            <HelpPanel />
        </BrowserRouter>
    );
}