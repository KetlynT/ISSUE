const InputGroup = ({ label, name, value, onChange, type = "text", placeholder }) => (
    <div>
        <label className="block text-sm font-semibold text-gray-700 mb-1">{label}</label>
        <input 
            type={type} name={name} value={value || ''} onChange={onChange} placeholder={placeholder}
            className="w-full border border-gray-300 px-4 py-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-colors"
        />
    </div>
);