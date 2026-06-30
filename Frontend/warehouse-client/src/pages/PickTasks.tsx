import { useEffect, useState } from 'react';
import axiosClient from '../api/axiosClient';

interface PickTask {
    id: string;
    sector: string;
    status: string;
    assignedWorkerId: string | null;
}

export default function PickTasks() {
    const [tasks, setTasks] = useState<PickTask[]>([]);
    const [loading, setLoading] = useState<boolean>(true);

    useEffect(() => {
        const fetchTasks = async () => {
            try{
                const response = await axiosClient.get('/PickTask');
                setTasks(response.data);
            } catch (error) {
                console.error("Error", error);
            } finally {
                setLoading(false);
            }
        };
        fetchTasks();
    }, []);

    if (loading) {
        return <h2 style={{ color: 'white', textAlign: 'center', marginTop: '50px' }}>Загрузка заданий...</h2>;
    }

    return (
        <div style={{ backgroundColor: '#121212', minHeight: '100vh', color: '#e0e0e0', padding: '20px' }}>
            <h2 style={{ textAlign: 'center', marginBottom: '30px' }}>Задания на сборку</h2>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', alignItems: 'center' }}>
                {
                    tasks.map(task => (
                        <div key={task.id}>
                            <p>Sector: {task.sector}, Status: {task.status}</p>
                        </div>
                    ))
                }
            </div>
        </div>
    );
}