import PropTypes from 'prop-types';

export const InputGroup = ({ label, name, value, onChange, type = "text", placeholder, readOnly }) => (
    <div>
        <label className="block text-sm font-semibold text-gray-700 mb-1">{label}</label>
        <input 
            type={type} 
            name={name} 
            value={value || ''} 
            onChange={onChange} 
            placeholder={placeholder}
            readOnly={readOnly}
            className="w-full border border-gray-300 px-4 py-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-colors read-only:bg-gray-100 read-only:text-gray-500"
        />
    </div>
);

InputGroup.propTypes = {
    label: PropTypes.string.isRequired,
    name: PropTypes.string.isRequired,
    value: PropTypes.oneOfType([PropTypes.string, PropTypes.number]),
    onChange: PropTypes.func.isRequired,
    type: PropTypes.string,
    placeholder: PropTypes.string,
    readOnly: PropTypes.bool
};