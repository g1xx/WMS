import type { PickTask } from '../../types/task';

interface Props {
    task: PickTask;
    containerBarcode: string;
    setContainerBarcode: (val: string) => void;
    onStartTask: () => void;

}

export default function NewTaskScreen({ task, containerBarcode, setContainerBarcode, onStartTask }: Props) {
    return (
        <div style={{ backgroundColor: '#1e1e1e', padding: '20px', borderRadius: '8px', width: '90%', maxWidth: '400px', boxShadow: '0 4px 15px rgba(0,0,0,0.5)' }}>
            <h3 style={{ margin: '0 0 10px 0', color: '#4CAF50' }}>Task: {task.id.substring(0, 8)}...</h3>
            <p style={{ fontSize: '1.1rem' }}><strong>Sector:</strong> {task.sector}</p>
            <p style={{ fontSize: '1.1rem' }}><strong>Status:</strong> {task.status}</p>
            
            <hr style={{ borderColor: '#333', margin: '15px 0' }} />
            
            <h4>Picking Route ({task.items.length} items):</h4>
            
            {task.items.map(item => (
                <div key={item.id} style={{ borderLeft: '4px solid #4CAF50', paddingLeft: '10px', marginBottom: '15px', backgroundColor: '#2a2a2a', padding: '10px' }}>
                    <p style={{ margin: '5px 0', fontSize: '1.2rem' }}><strong>Location:</strong> <span style={{ color: '#64b5f6' }}>{item.locationBarcode}</span></p>
                    <p style={{ margin: '5px 0' }}><strong>Product:</strong> {item.productName}</p>
                    <p style={{ margin: '5px 0', color: '#a0a0a0' }}>SKU: {item.productSku}</p>
                    <p style={{ margin: '5px 0', fontSize: '1.2rem', color: '#ffeb3b' }}><strong>Pick: {item.requiredQuantity} pcs.</strong></p>
                </div>
            ))}

            <div style={{ marginTop: '20px', width: '100%' }}>
                <input 
                    type="text" 
                    placeholder="Scan Container Barcode"
                    value={containerBarcode}
                    onChange={(e) => setContainerBarcode(e.target.value)}
                    style={{ width: '100%', padding: '12px', boxSizing: 'border-box', marginBottom: '10px', borderRadius: '4px', border: '1px solid #555', backgroundColor: '#333', color: 'white' }}
                />
                <button 
                    onClick={onStartTask}
                    style={{ width: '100%', padding: '15px', fontSize: '1.1rem', backgroundColor: '#2196F3', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: 'bold' }}>
                    Start Task
                </button>
            </div>
        </div>
    );
}