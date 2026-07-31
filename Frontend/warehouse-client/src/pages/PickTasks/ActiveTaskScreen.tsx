import { useState, useEffect } from 'react';
import type { PickTask } from '../../types/task';
import axiosClient from '../../api/axiosClient';

interface Props {
    task: PickTask;
    scanLocation: string;
    setScanLocation: (val: string) => void;
    scanSku: string;
    setScanSku: (val: string) => void;
    scanQty: number;
    setScanQty: (val: number) => void;
    onPickItem: () => Promise<void>;
    onDispatch: (containerBarcode: string, conveyorBarcode: string) => Promise<void>;
    onCancel: () => Promise<void>;
}

export default function ActiveTaskScreen({
    task, scanLocation, setScanLocation, scanSku, setScanSku, scanQty, setScanQty, onPickItem, onDispatch, onCancel
}: Props) {
    const [step, setStep] = useState<number>(1);
    const [localError, setLocalError] = useState<string>('');
    const [isMenuOpen, setIsMenuOpen] = useState<boolean>(false);

    const [isDispatchMode, setIsDispatchMode] = useState<boolean>(false);
    const [dispatchContainer, setDispatchContainer] = useState<string>('');
    const [dispatchConveyor, setDispatchConveyor] = useState<string>('');

    const [isOverviewOpen, setIsOverviewOpen] = useState<boolean>(false);

    const [isMissingMode, setIsMissingMode] = useState<boolean>(false);
    const [missingQty, setMissingQty] = useState<number>(1);
    const [brigadierBadge, setBrigadierBadge] = useState<string>('');

    const currentItem = task.items.find(item => item.pickedQuantity < item.requiredQuantity);
    const hasPickedItems = task.items.some(item => item.pickedQuantity > 0);

    useEffect(() => {
        if (!currentItem && !isMenuOpen) {
            setIsDispatchMode(true);
        }
    }, [currentItem, isMenuOpen]);

    useEffect(() => {
        const handleKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape' && !isDispatchMode) {
                // При нажатии Esc всегда возвращаемся в главное меню, сбрасывая подменю
                setIsMissingMode(false);
                setIsMenuOpen(prev => !prev);
            }
        };
        window.addEventListener('keydown', handleKeyDown);
        return () => window.removeEventListener('keydown', handleKeyDown);
    }, [isDispatchMode]);

    const handleLocationNext = () => {
        if (scanLocation.trim() === currentItem?.locationBarcode) {
            setStep(2); setLocalError('');
        } else {
            setLocalError(`Неверная ячейка! Идите к: ${currentItem?.locationBarcode}`);
        }
    };

    const handleSkuNext = () => {
        if (scanSku.trim() === currentItem?.productSku) {
            setStep(3); setLocalError('');
            setScanQty(currentItem.requiredQuantity - currentItem.pickedQuantity);
        } else {
            setLocalError(`Неверный товар! Ожидается: ${currentItem?.productSku}`);
        }
    };

    const handleConfirm = async () => {
        await onPickItem();
        setStep(1);
        setLocalError('');
    };

    const handleDispatchSubmit = () => {
        if (!dispatchContainer.trim() || !dispatchConveyor.trim()) {
            alert("Пожалуйста, отсканируйте контейнер и конвейер!");
            return;
        }

        if (task.containerBarcode && dispatchContainer.trim() !== task.containerBarcode) {
            alert(`Ошибка! Это чужая коробка.\nОжидается: ${task.containerBarcode}\nЭтот контейнер уже участвует в другом заказе!`);
            setDispatchContainer(''); // Очищаем поле, чтобы заставить отсканировать правильную
            return;
        }
        
        onDispatch(dispatchContainer, dispatchConveyor);
    };

    const handleMissingSubmit = async () => {
        if (!brigadierBadge.trim()) {
            alert("Отсканируйте бейдж бригадира!");
            return;
        }

        try {
            const response = await axiosClient.post(`/PickTask/${task.id}/report-missing`, {
                locationBarcode: currentItem?.locationBarcode,
                productSku: currentItem?.productSku,
                missingQuantity: missingQty,
                brigadierBarcode: brigadierBadge
            });

            // Жестко закрываем меню при успехе
            setIsMissingMode(false);
            setIsMenuOpen(false);
            setBrigadierBadge('');
            
            alert(response.data?.Message || "Недостача подтверждена.");
            
            // Здесь таска должна обновиться из родительского компонента
        } catch (error: any) {
            console.error("Ошибка списания:", error);
            alert(error.response?.data || "Ошибка подтверждения недостачи.");
        }
    };

    // ==========================================
    // ЭКРАН 1: ОТПРАВКА КОНТЕЙНЕРА 
    // ==========================================
    if (isDispatchMode) {
        return (
            <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)' }}>
                <h3 style={{ margin: '0 0 15px 0', color: '#ff9800', textAlign: 'center' }}>
                    {currentItem ? "Pełny pojemnik (Сдача части)" : "Задание завершено!"}
                </h3>
                <p style={{ color: '#aaa', marginBottom: '20px', textAlign: 'center' }}>Отсканируйте ТЕКУЩУЮ коробку и КОНВЕЙЕР.</p>

                <input type="text" autoFocus placeholder="1. Штрихкод КОРОБКИ..." value={dispatchContainer} onChange={(e) => setDispatchContainer(e.target.value)} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }} />
                <input type="text" placeholder="2. Штрихкод КОНВЕЙЕРА..." value={dispatchConveyor} onChange={(e) => setDispatchConveyor(e.target.value)} disabled={!dispatchContainer} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: dispatchContainer ? '#333' : '#222', color: 'white', fontSize: '1.1rem' }} />

                <button onClick={handleDispatchSubmit} disabled={!dispatchContainer || !dispatchConveyor} style={{ width: '100%', padding: '15px', backgroundColor: (dispatchContainer && dispatchConveyor) ? '#4CAF50' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold', fontSize: '1.1rem' }}>
                    Подтвердить отправку
                </button>

                {currentItem && (
                    <button onClick={() => { setIsDispatchMode(false); setIsMenuOpen(false); }} style={{ width: '100%', padding: '10px', marginTop: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                        Отмена (Вернуться к сборке)
                    </button>
                )}
            </div>
        );
    }

    // ==========================================
    // ЭКРАН 2: СТАНДАРТНАЯ СБОРКА ТОВАРА
    // ==========================================
    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)', position: 'relative' }}>

            <button 
                onClick={() => setIsMenuOpen(true)}
                style={{ position: 'absolute', top: '15px', right: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', padding: '5px 10px', cursor: 'pointer', fontSize: '0.8rem', zIndex: 5 }}
            >
                ESC (Меню)
            </button>

            <h3 style={{ margin: '0 0 10px 0', color: '#4CAF50' }}>Task: {task.id.substring(0, 8)}...</h3>
            <h4 style={{ color: '#ff9800', marginTop: '20px' }}>Текущий товар:</h4>

            <div style={{ borderLeft: '4px solid #ffeb3b', paddingLeft: '10px', marginBottom: '15px', backgroundColor: '#2a2a2a', padding: '15px' }}>
                <p style={{ margin: '5px 0', fontSize: '1.4rem' }}><strong>Ячейка:</strong> <span style={{ color: '#64b5f6' }}>{currentItem?.locationBarcode}</span></p>
                <p style={{ margin: '5px 0', fontSize: '1.2rem' }}><strong>Товар:</strong> {currentItem?.productName}</p>
                <p style={{ margin: '5px 0', color: '#a0a0a0' }}>SKU: {currentItem?.productSku}</p>
                <p style={{ margin: '10px 0 5px 0', fontSize: '1.3rem', color: '#ffeb3b' }}>
                    <strong>Собрать: {currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 0} шт</strong>
                </p>
            </div>

            <div style={{ marginTop: '20px', width: '100%', backgroundColor: '#2a2a2a', padding: '15px', borderRadius: '8px', boxSizing: 'border-box' }}>
                {localError && (
                    <div style={{ backgroundColor: '#ff5252', color: 'white', padding: '10px', borderRadius: '4px', marginBottom: '15px', fontWeight: 'bold', textAlign: 'center' }}>
                        {localError}
                    </div>
                )}

                {step === 1 && (
                    <>
                        <input type="text" placeholder="Штрихкод ячейки..." value={scanLocation} onChange={(e) => setScanLocation(e.target.value)} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }} />
                        <button onClick={handleLocationNext} disabled={!scanLocation} style={{ width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: scanLocation ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: scanLocation ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}>Проверить ячейку</button>
                    </>
                )}

                {step === 2 && (
                    <>
                        <input type="text" placeholder="Артикул товара..." value={scanSku} onChange={(e) => setScanSku(e.target.value)} style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '15px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem' }} />
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={() => setStep(1)} style={{ flex: 1, padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Назад</button>
                            <button onClick={handleSkuNext} disabled={!scanSku} style={{ flex: 2, padding: '15px', backgroundColor: scanSku ? '#2196F3' : '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: scanSku ? 'pointer' : 'not-allowed', fontWeight: 'bold' }}>Проверить артикул</button>
                        </div>
                    </>
                )}

                {step === 3 && (
                    <>
                        <div style={{ display: 'flex', gap: '10px', marginBottom: '15px' }}>
                            <label style={{ alignSelf: 'center', fontWeight: 'bold', fontSize: '1.2rem' }}>Кол-во:</label>
                            <input type="number" min="1" max={currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 1} value={scanQty} onChange={(e) => setScanQty(Number(e.target.value))} style={{ flex: 1, padding: '12px', boxSizing: 'border-box', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }} />
                        </div>
                        <div style={{ display: 'flex', gap: '10px' }}>
                            <button onClick={() => setStep(2)} style={{ flex: 1, padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Назад</button>
                            <button onClick={handleConfirm} style={{ flex: 2, padding: '15px', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>В коробку</button>
                        </div>
                    </>
                )}
            </div>

            {/* СПИСОК ТОВАРОВ (OVERVIEW) */}
            {isOverviewOpen && (
                <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', backgroundColor: '#1e1e1e', borderRadius: '8px', display: 'flex', flexDirection: 'column', padding: '20px', boxSizing: 'border-box', zIndex: 20 }}>
                    <h3 style={{ color: '#4CAF50', margin: '0 0 15px 0', textAlign: 'center' }}>Обзор заказа</h3>
                    <div style={{ flex: 1, overflowY: 'auto', marginBottom: '15px', paddingRight: '5px' }}>
                        {task.items.map(item => {
                            const isDone = item.pickedQuantity >= item.requiredQuantity;
                            const isPartial = item.pickedQuantity > 0 && !isDone;
                            return (
                                <div key={item.id} style={{ borderLeft: `5px solid ${isDone ? '#4CAF50' : isPartial ? '#ff9800' : '#555'}`, backgroundColor: '#2a2a2a', padding: '12px', marginBottom: '10px', borderRadius: '4px' }}>
                                    <p style={{ margin: '0 0 5px 0', fontSize: '1.1rem' }}><strong>Loc:</strong> <span style={{ color: '#64b5f6' }}>{item.locationBarcode}</span></p>
                                    <p style={{ margin: '0 0 5px 0' }}>{item.productName}</p>
                                    <p style={{ margin: '0 0 8px 0', color: '#aaa', fontSize: '0.9rem' }}>SKU: {item.productSku}</p>
                                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                                        <span style={{ fontSize: '1.2rem', fontWeight: 'bold', color: isDone ? '#4CAF50' : '#fff' }}>Собрано: {item.pickedQuantity} / {item.requiredQuantity}</span>
                                        {isDone && <span style={{ color: '#4CAF50', fontWeight: 'bold' }}>✓ Готово</span>}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                    <button onClick={() => setIsOverviewOpen(false)} style={{ width: '100%', padding: '15px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', cursor: 'pointer', fontWeight: 'bold' }}>Вернуться к сборке</button>
                </div>
            )}

            {/* МЕНЮ ESC (Теперь содержит оба состояния внутри одного родителя) */}
            {isMenuOpen && (
                <div style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', backgroundColor: 'rgba(0,0,0,0.9)', borderRadius: '8px', display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', padding: '20px', boxSizing: 'border-box', zIndex: 10 }}>
                    
                    {!isMissingMode ? (
                        <>
                            <h3 style={{ color: '#ff5252', marginBottom: '25px', textAlign: 'center' }}>Меню исключений</h3>

                            <button onClick={() => { setIsDispatchMode(true); setIsMenuOpen(false); }} style={{ width: '100%', padding: '15px', backgroundColor: '#ff9800', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                📦 Full Container
                            </button>

                            <button onClick={() => { setIsOverviewOpen(true); setIsMenuOpen(false); }} style={{ width: '100%', padding: '15px', backgroundColor: '#4CAF50', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                📋 Список товаров
                            </button>

                            <button onClick={() => alert(`Ожидаемый остаток в ячейке:\nЯчейка: ${currentItem?.locationBarcode}\nТовар: ${currentItem?.productSku}\nОстаток: ${currentItem?.availableStock} шт.`)} style={{ width: '100%', padding: '15px', backgroundColor: '#2196F3', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                🔍 Проверить остаток на полке
                            </button>

                            <button onClick={() => {
                                setIsMissingMode(true);
                                // АВТОЗАПОЛНЕНИЕ: сразу подставляем то, что осталось собрать
                                if (currentItem) {
                                    setMissingQty(currentItem.requiredQuantity - currentItem.pickedQuantity);
                                }
                            }} style={{ width: '100%', padding: '15px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', fontWeight: 'bold', marginBottom: '12px', cursor: 'pointer' }}>
                                ❌ Не удалось найти
                            </button>

                            <button onClick={() => { setIsMenuOpen(false); onCancel(); }} disabled={hasPickedItems} style={{ width: '100%', padding: '15px', backgroundColor: hasPickedItems ? '#333' : '#f44336', color: hasPickedItems ? '#777' : 'white', border: 'none', borderRadius: '6px', fontSize: '1.1rem', marginBottom: '12px', cursor: hasPickedItems ? 'not-allowed' : 'pointer' }}>
                                🚫 Отменить задание {hasPickedItems && '(Недоступно)'}
                            </button>

                            <button onClick={() => setIsMenuOpen(false)} style={{ width: '60%', padding: '10px', marginTop: '30px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
                                Закрыть (Esc)
                            </button>
                        </>
                    ) : (
                        // ПОДМЕНЮ: Списание недостачи
                        <div style={{ width: '100%', textAlign: 'center' }}>
                            <h3 style={{ color: '#ff5252', marginBottom: '15px' }}>Подтверждение недостачи</h3>

                            <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Будет списано (шт):</p>
                            <input
                                type="number"
                                min="1"
                                max={currentItem ? currentItem.requiredQuantity - currentItem.pickedQuantity : 1}
                                value={missingQty}
                                onChange={(e) => setMissingQty(Number(e.target.value))}
                                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.2rem', textAlign: 'center' }}
                            />

                            <p style={{ color: '#aaa', margin: '0 0 10px 0' }}>Отсканируйте бейдж Бригадира:</p>
                            <input
                                type="text"
                                autoFocus
                                placeholder="Штрихкод бригадира..."
                                value={brigadierBadge}
                                onChange={(e) => setBrigadierBadge(e.target.value)}
                                style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '20px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white', fontSize: '1.1rem', textAlign: 'center' }}
                            />

                            <div style={{ display: 'flex', gap: '10px' }}>
                                <button onClick={() => setIsMissingMode(false)} style={{ flex: 1, padding: '12px', backgroundColor: '#555', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>Отмена</button>
                                <button onClick={handleMissingSubmit} style={{ flex: 2, padding: '12px', backgroundColor: '#e91e63', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>Подтвердить</button>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}