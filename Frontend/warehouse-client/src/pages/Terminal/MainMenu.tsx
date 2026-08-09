import { useNavigate } from 'react-router-dom';
import { logout } from '../../api/axiosClient';

interface Props {
    onStartPicking: () => void;
    onStartPutaway: () => void;
    onChangeSector: () => void;
}

export default function MainMenu({ onStartPicking, onStartPutaway, onChangeSector }: Props) {
    const navigate = useNavigate();

    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '30px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', textAlign: 'center' }}>
            <h2 style={{ color: '#4CAF50', marginTop: 0 }}>Warehouse Terminal</h2>

            <button
                onClick={onStartPicking}
                style={{ width: '100%', padding: '18px', fontSize: '1.2rem', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold', marginBottom: '12px' }}
            >
                Start Picking
            </button>

            <button
                onClick={onStartPutaway}
                style={{ width: '100%', padding: '18px', fontSize: '1.2rem', backgroundColor: '#ff9800', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold', marginBottom: '12px' }}
            >
                Start Putaway
            </button>

            <button
                onClick={onChangeSector}
                style={{ width: '100%', padding: '15px', fontSize: '1rem', backgroundColor: '#2196F3', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold' }}
            >
                Change Sector
            </button>

            <button
                onClick={() => navigate('/admin/inventory')}
                style={{ width: '100%', padding: '10px', marginTop: '20px', backgroundColor: 'transparent', color: '#888', border: '1px solid #444', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85rem' }}
            >
                Admin: Inventory
            </button>

            <button
                onClick={logout}
                style={{ width: '100%', padding: '10px', marginTop: '10px', backgroundColor: 'transparent', color: '#f44336', border: '1px solid #663333', borderRadius: '4px', cursor: 'pointer', fontSize: '0.85rem' }}
            >
                Logout
            </button>
        </div>
    );
}
