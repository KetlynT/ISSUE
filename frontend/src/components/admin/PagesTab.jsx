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

    useEffect(() => { ContentService.getAllPages().then(setPages); }, []);

    const handleEdit = async (page) => {
        const fullPage = await ContentService.getPage(page.slug);
        setSelectedPage(fullPage);
    };

    const handleSave = async (e) => {
        e.preventDefault();
        setLoading(true);
        try {
            await ContentService.updatePage(selectedPage.id, selectedPage);
            toast.success("Página salva!");
            ContentService.getAllPages().then(setPages);
        } catch(e) {
            toast.error("Erro ao salvar página.");
        } finally {
            setLoading(false);
        }
    };

    const modules = { toolbar: [ [{ 'header': [1, 2, 3, false] }], ['bold', 'italic', 'underline'], [{'list': 'ordered'}, {'list': 'bullet'}], ['link', 'clean'] ] };

    return (
        <div className="grid md:grid-cols-3 gap-8">
            <div className="bg-white rounded-lg shadow border p-4 h-fit">
                <h3 className="font-bold border-b pb-2 mb-2 text-gray-700">Selecione uma Página</h3>
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
                        <InputGroup label="Título da Página" name="title" value={selectedPage.title} onChange={e => setSelectedPage({...selectedPage, title: e.target.value})} />
                        <div>
                            <label className="block text-sm font-bold mb-2 text-gray-700">Conteúdo</label>
                            <ReactQuill theme="snow" value={selectedPage.content} onChange={(value) => setSelectedPage({...selectedPage, content: value})} modules={modules} className="h-64 mb-12" />
                        </div>
                        <div className="pt-4"><Button type="submit" isLoading={loading}>Salvar Alterações</Button></div>
                    </form>
                ) : (
                    <div className="text-center text-gray-400 py-20">Selecione uma página para editar.</div>
                )}
            </div>
        </div>
    );
};