import { useState, useEffect } from 'react';
import { AddressService } from '../services/addressService';
import { Button } from './ui/Button';
import { Plus, Edit, Trash2, MapPin, Star, X, Save } from 'lucide-react';
import toast from 'react-hot-toast';
import PropTypes from 'prop-types';

export const AddressManager = ({ onUpdate, allowSelection = false, onSelect }) => {
  const [addresses, setAddresses] = useState([]);
  const [loading, setLoading] = useState(true);
  
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingAddress, setEditingAddress] = useState(null);
  const [addressForm, setAddressForm] = useState(initialAddressState());

  useEffect(() => {
    loadAddresses();
  }, []);

  const loadAddresses = async () => {
    try {
      setLoading(true);
      const data = await AddressService.getAll();
      setAddresses(data);
      if (onUpdate) onUpdate(data); 
    } catch (error) {
      toast.error("Erro ao carregar endereços.");
    } finally {
      setLoading(false);
    }
  };

  function initialAddressState() {
    return {
      name: 'Casa',
      receiverName: '',
      zipCode: '',
      street: '',
      number: '',
      complement: '',
      neighborhood: '',
      city: '',
      state: '',
      reference: '',
      phoneNumber: '',
      isDefault: false
    };
  }

  const formatCEP = (value) => {
    return value.replace(/\D/g, '').replace(/^(\d{5})(\d)/, '$1-$2').slice(0, 9);
  };

  const formatPhone = (value) => {
    const v = value.replace(/\D/g, '');
    if (v.length > 10) return v.replace(/^(\d{2})(\d{5})(\d{4}).*/, '($1) $2-$3');
    if (v.length > 5) return v.replace(/^(\d{2})(\d{4})(\d{0,4}).*/, '($1) $2-$3');
    if (v.length > 2) return v.replace(/^(\d{2})(\d{0,5}).*/, '($1) $2');
    return v;
  };

  const handleOpenForm = (address = null) => {
    if (address) {
      setEditingAddress(address);
      setAddressForm(address);
    } else {
      setEditingAddress(null);
      setAddressForm(initialAddressState());
    }
    setIsFormOpen(true);
  };

  const handleSaveAddress = async (e) => {
    e.preventDefault();
    const toastId = toast.loading("Salvando...");
    try {
      if (editingAddress) {
        await AddressService.update(editingAddress.id, addressForm);
      } else {
        await AddressService.create(addressForm);
      }
      toast.success("Endereço salvo!", { id: toastId });
      setIsFormOpen(false);
      loadAddresses();
    } catch (error) {
      toast.error("Erro ao salvar.", { id: toastId });
    }
  };

  const handleDeleteAddress = async (id) => {
    if (!window.confirm("Excluir este endereço?")) return;
    try {
      await AddressService.delete(id);
      toast.success("Endereço removido.");
      loadAddresses();
    } catch (e) {
      toast.error("Erro ao excluir.");
    }
  };

  const handleCepBlur = async () => {
    if (addressForm.zipCode.length >= 8) {
      try {
        const cleanCep = addressForm.zipCode.replace(/\D/g, '');
        const res = await fetch(`https://viacep.com.br/ws/${cleanCep}/json/`);
        const data = await res.json();
        if (!data.erro) {
          setAddressForm(prev => ({
            ...prev,
            street: data.logradouro,
            neighborhood: data.bairro,
            city: data.localidade,
            state: data.uf
          }));
        }
      } catch (e) { console.error("Erro CEP", e); }
    }
  };

  if (loading) return <div className="p-4 text-center text-sm text-gray-500">Carregando endereços...</div>;

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h3 className="font-bold text-gray-700 flex items-center gap-2"><MapPin size={18}/> Endereços Cadastrados</h3>
        <Button size="sm" onClick={() => handleOpenForm()} variant="outline" className="text-primary border-primary/30 hover:bg-primary/5">
          <Plus size={16} /> Novo
        </Button>
      </div>

      <div className="grid gap-3 max-h-[60vh] overflow-y-auto pr-1">
        {addresses.map(addr => (
          <div key={addr.id} className={`bg-white p-4 rounded-lg border transition-all ${addr.isDefault ? 'border-primary/50 bg-primary/5' : 'border-gray-200'}`}>
            <div className="flex justify-between items-start">
              <div className="cursor-pointer flex-grow" onClick={() => allowSelection && onSelect && onSelect(addr)}>
                <div className="flex items-center gap-2 mb-1">
                    <span className="font-bold text-gray-800">{addr.name}</span>
                    {addr.isDefault && <span className="bg-primary/10 text-primary text-[10px] px-2 py-0.5 rounded-full font-bold flex items-center gap-1"><Star size={10} fill="currentColor"/> Padrão</span>}
                </div>
                <p className="text-gray-600 text-xs">
                  {addr.street}, {addr.number} {addr.complement}
                </p>
                <p className="text-gray-600 text-xs">
                  {addr.neighborhood}, {addr.city} - {addr.state}
                </p>
                <p className="text-gray-500 text-[10px] mt-1">CEP: {addr.zipCode}</p>
              </div>
              
              <div className="flex gap-1 ml-2">
                <button onClick={() => handleOpenForm(addr)} className="p-1.5 text-gray-400 hover:text-primary hover:bg-primary/10 rounded"><Edit size={16}/></button>
                <button onClick={() => handleDeleteAddress(addr.id)} className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded"><Trash2 size={16}/></button>
              </div>
            </div>
          </div>
        ))}
        {addresses.length === 0 && <div className="text-center py-6 text-gray-400 text-sm border-2 border-dashed rounded-lg">Nenhum endereço encontrado.</div>}
      </div>

      {isFormOpen && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm z-[60] flex items-center justify-center p-4">
          <div className="bg-white rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto animate-in fade-in zoom-in duration-200">
            <div className="p-5 border-b flex justify-between items-center sticky top-0 bg-white z-10">
              <h3 className="font-bold text-gray-800">{editingAddress ? 'Editar' : 'Novo'} Endereço</h3>
              <button onClick={() => setIsFormOpen(false)}><X className="text-gray-400 hover:text-gray-600"/></button>
            </div>
            
            <form onSubmit={handleSaveAddress} className="p-5 space-y-3">
              <div className="grid grid-cols-2 gap-3">
                <div className="col-span-2"><label className="text-xs font-bold text-gray-500">Nome (Ex: Casa)</label><input className="input-base" required value={addressForm.name} onChange={e => setAddressForm({...addressForm, name: e.target.value})} /></div>
                <div className="col-span-2"><label className="text-xs font-bold text-gray-500">Quem recebe?</label><input className="input-base" required value={addressForm.receiverName} onChange={e => setAddressForm({...addressForm, receiverName: e.target.value})} /></div>
                
                <div>
                    <label className="text-xs font-bold text-gray-500">CEP</label>
                    <input className="input-base" required value={addressForm.zipCode} 
                        onChange={e => setAddressForm({...addressForm, zipCode: formatCEP(e.target.value)})} 
                        onBlur={handleCepBlur}
                        maxLength={9}
                    />
                </div>
                <div>
                    <label className="text-xs font-bold text-gray-500">Telefone</label>
                    <input className="input-base" required value={addressForm.phoneNumber} 
                        onChange={e => setAddressForm({...addressForm, phoneNumber: formatPhone(e.target.value)})}
                        maxLength={15}
                    />
                </div>
                
                <div className="col-span-2"><label className="text-xs font-bold text-gray-500">Rua</label><input className="input-base" required value={addressForm.street} onChange={e => setAddressForm({...addressForm, street: e.target.value})} /></div>
                
                <div><label className="text-xs font-bold text-gray-500">Número</label><input className="input-base" required value={addressForm.number} onChange={e => setAddressForm({...addressForm, number: e.target.value})} /></div>
                <div><label className="text-xs font-bold text-gray-500">Comp.</label><input className="input-base" value={addressForm.complement} onChange={e => setAddressForm({...addressForm, complement: e.target.value})} /></div>
                
                <div><label className="text-xs font-bold text-gray-500">Bairro</label><input className="input-base" required value={addressForm.neighborhood} onChange={e => setAddressForm({...addressForm, neighborhood: e.target.value})} /></div>
                <div className="grid grid-cols-3 gap-2">
                    <div className="col-span-2"><label className="text-xs font-bold text-gray-500">Cidade</label><input className="input-base" required value={addressForm.city} onChange={e => setAddressForm({...addressForm, city: e.target.value})} /></div>
                    <div><label className="text-xs font-bold text-gray-500">UF</label><input className="input-base uppercase" required maxLength={2} value={addressForm.state} onChange={e => setAddressForm({...addressForm, state: e.target.value})} /></div>
                </div>
              </div>

              <div className="flex items-center gap-2 pt-2">
                <input type="checkbox" id="def_modal" className="w-4 h-4" checked={addressForm.isDefault} onChange={e => setAddressForm({...addressForm, isDefault: e.target.checked})} />
                <label htmlFor="def_modal" className="text-sm">Endereço padrão</label>
              </div>

              <div className="flex justify-end gap-2 pt-4">
                <Button type="button" variant="ghost" onClick={() => setIsFormOpen(false)}>Cancelar</Button>
                <Button type="submit"><Save size={16}/> Salvar</Button>
              </div>
            </form>
          </div>
        </div>
      )}
      
      <style>{`.input-base { width: 100%; border: 1px solid #e5e7eb; border-radius: 0.5rem; padding: 0.5rem; font-size: 0.875rem; outline: none; transition: border-color 0.2s; } .input-base:focus { border-color: var(--color-primary); ring: 2px solid var(--color-primary); }`}</style>
    </div>
  );
};

AddressManager.propTypes = {
  onUpdate: PropTypes.func,
  allowSelection: PropTypes.bool,
  onSelect: PropTypes.func
};