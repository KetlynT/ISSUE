import { useEffect, useState } from 'react';
import { ProductService } from '../../services/productService';
import { Button } from '../../components/ui/Button';
import toast from 'react-hot-toast';
import { ContentService } from '../../services/contentService';
import { InputGroup } from '../ui/InputGroup';

const SettingsTab = () => {
    const [formData, setFormData] = useState({});
    const [heroImageFile, setHeroImageFile] = useState(null);
    const [logoFile, setLogoFile] = useState(null); 
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const load = async () => {
            try {
                const data = await ContentService.getSettings();

                if (!data.purchase_enabled) {
                    data.purchase_enabled = 'true';
                }
                
                setFormData(data);
            } catch (error) {
                console.error("Erro ao carregar settings", error);
            }
        };
        load();
    }, []);

    const handleChange = (e) => {
        setFormData({ ...formData, [e.target.name]: e.target.value });
    };

    const handleSave = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            let updatedData = { ...formData };
            if (heroImageFile) updatedData.hero_bg_url = await ProductService.uploadImage(heroImageFile);
            if (logoFile) updatedData.site_logo = await ProductService.uploadImage(logoFile);

            await ContentService.saveSettings(updatedData);
            
            toast.success("Configurações atualizadas!");
        } catch (e) {
            toast.error("Erro ao salvar configurações.");
        } finally {
            setLoading(false);
        }
    };

    return (
        <form onSubmit={handleSave} className="bg-white p-8 rounded-xl shadow-sm border border-gray-100 max-w-4xl mx-auto space-y-8">
            
            <div className="bg-yellow-50 border border-yellow-200 p-4 rounded-lg mb-6">
                <h3 className="font-bold text-lg text-yellow-800 border-b border-yellow-200 pb-2 mb-4">Controle da Loja</h3>
                <div className="flex items-center gap-4">
                    <label className="flex items-center cursor-pointer relative">
                        <input 
                            type="checkbox" 
                            name="purchase_enabled" 
                            checked={formData.purchase_enabled !== 'false'} 
                            onChange={e => setFormData({...formData, purchase_enabled: e.target.checked ? 'true' : 'false'})}
                            className="sr-only peer"
                        />
                        <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
                        <span className="ml-3 text-sm font-medium text-gray-900">
                            {formData.purchase_enabled !== 'false' ? 'Compras e Cadastros ATIVADOS' : 'MODO SOMENTE ORÇAMENTO (Compras Desativadas)'}
                        </span>
                    </label>
                </div>
                <p className="text-sm text-gray-600 mt-2">
                    Quando desativado, clientes não poderão se cadastrar, fazer login (exceto Admin), adicionar itens ao carrinho ou finalizar compras. Apenas a opção de orçamento via WhatsApp ficará visível.
                </p>
            </div>

            <div className="space-y-4">
                <h3 className="font-bold text-lg text-gray-800 border-b pb-2">Identidade e Contato</h3>
                <div className="grid md:grid-cols-2 gap-4">
                    <InputGroup label="Nome do Negócio" name="site_name" value={formData.site_name} onChange={handleChange} placeholder="Ex: Minha Gráfica" />
                    <InputGroup label="Email de Contato" name="contact_email" value={formData.contact_email} onChange={handleChange} />
                    <InputGroup label="WhatsApp (Números)" name="whatsapp_number" value={formData.whatsapp_number} onChange={handleChange} placeholder="5511999999999" />
                    <InputGroup label="WhatsApp (Visível)" name="whatsapp_display" value={formData.whatsapp_display} onChange={handleChange} placeholder="(11) 99999-9999" />
                    <InputGroup label="CEP de Origem (Frete)" name="sender_cep" value={formData.sender_cep} onChange={handleChange} />
                    <div className="md:col-span-2">
                        <InputGroup label="Endereço Físico" name="address" value={formData.address} onChange={handleChange} placeholder="Rua Exemplo, 123 - Cidade/UF" />
                    </div>
                </div>
            </div>

            <div className="space-y-4">
                <h3 className="font-bold text-lg text-gray-800 border-b pb-2">Cabeçalho (Hero)</h3>
                <div className="grid md:grid-cols-2 gap-4">
                    <InputGroup label="Badge (Destaque)" name="hero_badge" value={formData.hero_badge} onChange={handleChange} />
                    <InputGroup label="Título Principal" name="hero_title" value={formData.hero_title} onChange={handleChange} />
                    <div className="md:col-span-2">
                        <InputGroup label="Subtítulo" name="hero_subtitle" value={formData.hero_subtitle} onChange={handleChange} />
                    </div>
                    <div className="border-2 border-dashed border-gray-300 p-4 rounded-lg text-center"><label className="block text-sm font-bold mb-2">Logo do Site</label><input type="file" onChange={e => setLogoFile(e.target.files[0])} className="text-xs"/></div>
                    <div className="border-2 border-dashed border-gray-300 p-4 rounded-lg text-center"><label className="block text-sm font-bold mb-2">Fundo do Hero</label><input type="file" onChange={e => setHeroImageFile(e.target.files[0])} className="text-xs"/></div>
                </div>
            </div>

            <div className="space-y-4">
                <h3 className="font-bold text-lg text-gray-800 border-b pb-2">Rodapé</h3>
                <div>
                    <label className="block text-sm font-semibold text-gray-700 mb-1">Texto "Sobre a Empresa"</label>
                    <textarea 
                        name="footer_about" 
                        value={formData.footer_about || ''} 
                        onChange={handleChange}
                        className="w-full border border-gray-300 px-4 py-2 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none transition-colors h-24 resize-none"
                        placeholder="Escreva um breve resumo sobre a empresa para aparecer no rodapé..."
                    />
                </div>
            </div>

            <div className="space-y-4">
                <h3 className="font-bold text-lg text-gray-800 border-b pb-2">Personalização Visual</h3>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-2">Cor Primária</label>
                        <div className="flex items-center gap-2">
                            <input type="color" name="primary_color" value={formData.primary_color || '#2563eb'} onChange={handleChange} className="h-10 w-10 border-0 p-0 rounded cursor-pointer"/>
                            <span className="text-xs text-gray-500">{formData.primary_color}</span>
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-2">Cor Secundária</label>
                        <div className="flex items-center gap-2">
                            <input type="color" name="secondary_color" value={formData.secondary_color || '#1e40af'} onChange={handleChange} className="h-10 w-10 border-0 p-0 rounded cursor-pointer"/>
                            <span className="text-xs text-gray-500">{formData.secondary_color}</span>
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-2">Fundo Rodapé</label>
                        <div className="flex items-center gap-2">
                            <input type="color" name="footer_bg_color" value={formData.footer_bg_color || '#111827'} onChange={handleChange} className="h-10 w-10 border-0 p-0 rounded cursor-pointer"/>
                            <span className="text-xs text-gray-500">{formData.footer_bg_color}</span>
                        </div>
                    </div>
                    <div>
                        <label className="block text-sm font-semibold text-gray-700 mb-2">Texto Rodapé</label>
                        <div className="flex items-center gap-2">
                            <input type="color" name="footer_text_color" value={formData.footer_text_color || '#d1d5db'} onChange={handleChange} className="h-10 w-10 border-0 p-0 rounded cursor-pointer"/>
                            <span className="text-xs text-gray-500">{formData.footer_text_color}</span>
                        </div>
                    </div>
                </div>
            </div>

            <Button type="submit" className="w-full" isLoading={loading}>Salvar Todas as Configurações</Button>
        </form>
    );
};

export default SettingsTab;