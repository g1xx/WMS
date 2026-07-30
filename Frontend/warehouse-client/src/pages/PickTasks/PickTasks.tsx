import { useState, useEffect } from 'react';
import axiosClient from '../../api/axiosClient';
import type { PickTask } from '../../types/task';
import NewTaskScreen from './NewTaskScreen';
import ActiveTaskScreen from './ActiveTaskScreen';

export default function PickTasks() {
    const [task, setTask] = useState<PickTask | null>(null);
    const [loading, setLoading] = useState<boolean>(true);

    const [containerBarcode, setContainerBarcode] = useState<string>('');
    const [scanLocation, setScanLocation] = useState<string>('');
    const [scanSku, setScanSku] = useState<string>('');
    const [scanQty, setScanQty] = useState<number>(1);

   
    useEffect(() => {
        fetchNextTask();
    }, []);

    const handleStartTask = async () => {
        if (!task || !containerBarcode) {
            alert("Please scan a container barcode first!");
            return;
        }
        try {
            await axiosClient.post(`/PickTask/${task.id}/start`, {
                containerBarcode: containerBarcode
            });
            setTask({ ...task, status: 'InProgress' });
        } catch (error: any) {
            console.error("Error starting task:", error);
            alert(error.response?.data || "Failed to start task.");
            fetchNextTask();
        }
    };

    const fetchNextTask = async () => {
        setLoading(true);
        try {
            // Добавляем параметр времени, чтобы сбросить жесткий кэш браузера (решает Проблему 1)
            const response = await axiosClient.get(`/PickTask/next?t=${new Date().getTime()}`);
            setTask(response.data ? response.data : null);
            setContainerBarcode('');
        } catch (error) {
            console.error("Error fetching task:", error);
            alert("Failed to load task.");
        } finally {
            setLoading(false);
        }
    };

    const handlePickItem = async () => {
        if (!task) return;
        try {
            await axiosClient.post(`/PickTask/${task.id}/pick`, {
                locationBarcode: scanLocation,
                productSku: scanSku,
                quantity: scanQty
            });

            // Убрал alert, чтобы не кликать "ОК" после каждого товара (ускоряет работу)

            setScanLocation('');
            setScanSku('');
            setScanQty(1);

            // Обязательно ждем завершения запроса новых данных!
            await fetchNextTask();
        } catch (error: any) {
            console.error("Error picking item:", error);
            alert(error.response?.data || "Scan error!");
        }
    };

    // Решает Проблему 2 и 3: принимаем оба аргумента и шлем на новый эндпоинт
    const handleDispatch = async (containerBarcode: string, conveyorBarcode: string) => {
        if (!task) return;
        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/dispatch`, {
                containerBarcode: containerBarcode,
                conveyorBarcode: conveyorBarcode
            });

            alert(response.data?.message || "Контейнер успешно отправлен на конвейер.");

            await fetchNextTask();
        } catch (error: any) {
            console.error("Error dispatching task:", error);
            alert(error.response?.data || "Ошибка при закрытии контейнера");
        }
    };

    const handleCancelTask = async () => {
        if (!task) return;

        const confirmBox = window.confirm("Вы уверены, что хотите отказаться от задания? Коробка будет отвязана.");
        if (!confirmBox) return;

        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/cancel`);
            alert(response.data?.message || "Задание отменено.");
            await fetchNextTask();
        } catch (error: any) {
            console.error("Error canceling task:", error);
            alert(error.response?.data || "Ошибка при отмене задания.");
        }
    };

    if (loading) {
        return <h2 style={{ color: 'white', textAlign: 'center', marginTop: '50px' }}>Loading...</h2>;
    }

    return (
        <div style={{ backgroundColor: '#121212', minHeight: '100vh', color: '#e0e0e0', padding: '20px' }}>
            <h2 style={{ textAlign: 'center', marginBottom: '30px' }}>Picking Terminal</h2>

            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', alignItems: 'center' }}>
                {!task ? (
                    <button
                        onClick={fetchNextTask}
                        style={{ padding: '20px', fontSize: '1.2rem', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '8px', cursor: 'pointer', width: '90%', maxWidth: '400px', fontWeight: 'bold' }}
                    >
                        Get Next Task
                    </button>
                ) : task.status === 'New' ? (
                    <NewTaskScreen
                        task={task}
                        containerBarcode={containerBarcode}
                        setContainerBarcode={setContainerBarcode}
                        onStartTask={handleStartTask}
                    />
                ) : (
                    <ActiveTaskScreen
                        task={task}
                        scanLocation={scanLocation}
                        setScanLocation={setScanLocation}
                        scanSku={scanSku}
                        setScanSku={setScanSku}
                        scanQty={scanQty}
                        setScanQty={setScanQty}
                        onPickItem={handlePickItem}
                        onDispatch={handleDispatch}
                        onCancel={handleCancelTask}
                    />
                )}
            </div>
        </div>
    );
}