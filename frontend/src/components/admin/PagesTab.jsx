import { useEffect, useState } from 'react';
import { Button } from '../../components/ui/Button';
import toast from 'react-hot-toast';
import { ContentService } from '../../services/contentService';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';
import { InputGroup } from '../ui/InputGroup';

const PagesTab = () => {
    const [pages, setPages] = useState([]);
    const [selectedPage, setSelectedPage] = useState(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => { loadPages(); }, []);

    const loadPages = () => {
        ContentService.getAllPages().then(setPages);
    };

    const handleEdit = async (page) => {
        setLoading(true);
        try {
            const fullPage = await ContentService.getPage(page.slug);
            setSelectedPage(fullPage);
        } catch (error) {
            toast.error("Erro ao carregar detalhes da página.");
        } finally {
            setLoading(false);
        }
    };

    const handleCreateNew = () => {
        setSelectedPage({
            title: '',
            slug: '',
            content: ''
        });
    };

    const handleSave = async (e) => {
        e.preventDefault();
        
        if (!selectedPage.title || !selectedPage.slug) {
            toast.error("Título e Slug são obrigatórios.");
            return;
        }

        setLoading(true);
        try {
            if (selectedPage.id) {
                await ContentService.updatePage(selectedPage.slug, selectedPage);
                toast.success("Página atualizada com sucesso!");
            } else {
                await ContentService.createPage(selectedPage);
                toast.success("Página criada com sucesso!");
                setSelectedPage(null); 
            }
            loadPages();
        } catch(e) {
            console.error(e);
            toast.error("Erro ao salvar página. Verifique se o slug já existe.");
        } finally {
            setLoading(false);
        }
    };

    const modules = { toolbar: [ [{ 'header': [1, 2, 3, false] }], ['bold', 'italic', 'underline'], [{'list': 'ordered'}, {'list': 'bullet'}], ['link', 'clean'] ] };

    return (
        <div className="grid md:grid-cols-3 gap-8">
            <div className="bg-white rounded-lg shadow border p-4 h-fit">
                <div className="flex justify-between items-center border-b pb-2 mb-2">
                    <h3 className="font-bold text-gray-700">Páginas</h3>
                    <button 
                        onClick={handleCreateNew}
                        className="text-xs bg-blue-600 text-white px-2 py-1 rounded hover:bg-blue-700 transition"
                    >
                        + Nova
                    </button>
                </div>
                
                {pages.map(p => (
                    <div key={p.id} onClick={() => handleEdit(p)} className={`p-3 cursor-pointer border-b last:border-0 rounded transition-colors ${selectedPage?.id === p.id ? 'bg-blue-50 text-blue-700 border-blue-200' : 'hover:bg-gray-50'}`}>
                        <div className="font-medium">{p.title}</div>
                        <small className="text-gray-400 block text-xs">/{p.slug}</small>
                    </div>
                ))}
            </div>
            
            <div className="md:col-span-2 bg-white rounded-lg shadow border p-6">
                {selectedPage ? (
                    <form onSubmit={handleSave} className="space-y-6">
                        <div className="grid grid-cols-2 gap-4">
                            <InputGroup 
                                label="Título da Página" 
                                name="title" 
                                value={selectedPage.title} 
                                onChange={e => setSelectedPage({...selectedPage, title: e.target.value})} 
                            />
                            <InputGroup 
                                label="Slug (URL)" 
                                name="slug" 
                                value={selectedPage.slug} 
                                placeholder="ex: politica-de-troca"
                                readOnly={!!selectedPage.id} 
                                onChange={e => setSelectedPage({...selectedPage, slug: e.target.value})} 
                            />
                        </div>

                        <div>
                            <label className="block text-sm font-bold mb-2 text-gray-700">Conteúdo</label>
                            <ReactQuill 
                                theme="snow" 
                                value={selectedPage.content} 
                                onChange={(value) => setSelectedPage({...selectedPage, content: value})} 
                                modules={modules} 
                                className="h-64 mb-12" 
                            />
                        </div>
                        <div className="pt-4 flex gap-2">
                            <Button type="submit" isLoading={loading}>
                                {selectedPage.id ? 'Salvar Alterações' : 'Criar Página'}
                            </Button>
                            {selectedPage.id && (
                                <Button type="button" variant="outline" onClick={handleCreateNew}>
                                    Cancelar / Nova
                                </Button>
                            )}
                        </div>
                    </form>
                ) : (
                    <div className="text-center text-gray-400 py-20">
                        <p>Selecione uma página para editar</p>
                        <p>ou</p>
                        <button onClick={handleCreateNew} className="text-blue-600 hover:underline mt-2">Criar nova página</button>
                    </div>
                )}
            </div>
        </div>
    );
};

export default PagesTab;