import React, { useEffect, useState } from 'react';
import { ProductService } from '../services/productService';
import { AuthService } from '../services/authService';

export const AdminDashboard = () => {
  const [products, setProducts] = useState([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingProduct, setEditingProduct] = useState(null);

  // Estados do Formulário
  const [name, setName] = useState('');
  const [desc, setDesc] = useState('');
  const [price, setPrice] = useState('');
  const [imageFile, setImageFile] = useState(null);
  const [imageUrl, setImageUrl] = useState('');

  useEffect(() => {
    loadProducts();
  }, []);

  const loadProducts = async () => {
    try {
      const data = await ProductService.getAll();
      setProducts(data);
    } catch (error) {
      console.error("Erro ao carregar", error);
    }
  };

  const handleSave = async (e) => {
    e.preventDefault();
    try {
      let finalImageUrl = imageUrl;

      // Se tiver arquivo selecionado, faz upload primeiro
      if (imageFile) {
        finalImageUrl = await ProductService.uploadImage(imageFile);
      }

      const productData = {
        name,
        description: desc,
        price: parseFloat(price),
        imageUrl: finalImageUrl
      };

      if (editingProduct) {
        await ProductService.update(editingProduct.id, productData);
        alert('Produto atualizado!');
      } else {
        await ProductService.create(productData);
        alert('Produto criado!');
      }

      closeModal();
      loadProducts();
    } catch (error) {
      alert('Erro ao salvar produto');
      console.error(error);
    }
  };

  const handleDelete = async (id) => {
    if (window.confirm("Tem certeza que deseja excluir este produto?")) {
      try {
        await ProductService.delete(id);
        loadProducts();
      } catch (error) {
        alert("Erro ao excluir");
      }
    }
  };

  const openModal = (product = null) => {
    if (product) {
      setEditingProduct(product);
      setName(product.name);
      setDesc(product.description);
      setPrice(product.price);
      setImageUrl(product.imageUrl);
    } else {
      setEditingProduct(null);
      setName('');
      setDesc('');
      setPrice('');
      setImageUrl('');
    }
    setImageFile(null);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingProduct(null);
  };

  return (
    <div className="min-h-screen bg-gray-100">
      <nav className="bg-white shadow p-4 flex justify-between items-center">
        <h1 className="text-xl font-bold text-gray-800">Painel Administrativo</h1>
        <button onClick={AuthService.logout} className="text-red-500 hover:text-red-700">Sair</button>
      </nav>

      <div className="max-w-6xl mx-auto p-6">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold">Produtos</h2>
          <button 
            onClick={() => openModal()}
            className="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700"
          >
            + Novo Produto
          </button>
        </div>

        <div className="bg-white rounded-lg shadow overflow-hidden">
          <table className="min-w-full divide-y divide-gray-200">
            <thead className="bg-gray-50">
              <tr>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Produto</th>
                <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Preço</th>
                <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Ações</th>
              </tr>
            </thead>
            <tbody className="bg-white divide-y divide-gray-200">
              {products.map((prod) => (
                <tr key={prod.id}>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <div className="flex items-center">
                      <div className="h-10 w-10 flex-shrink-0">
                        <img className="h-10 w-10 rounded-full object-cover" src={prod.imageUrl || 'https://via.placeholder.com/40'} alt="" />
                      </div>
                      <div className="ml-4">
                        <div className="text-sm font-medium text-gray-900">{prod.name}</div>
                      </div>
                    </div>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    R$ {prod.price}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button onClick={() => openModal(prod)} className="text-indigo-600 hover:text-indigo-900 mr-4">Editar</button>
                    <button onClick={() => handleDelete(prod.id)} className="text-red-600 hover:text-red-900">Excluir</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {/* Modal de Criar/Editar */}
      {isModalOpen && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4">
          <div className="bg-white rounded-lg p-6 max-w-lg w-full">
            <h3 className="text-lg font-bold mb-4">{editingProduct ? 'Editar Produto' : 'Novo Produto'}</h3>
            <form onSubmit={handleSave} className="space-y-4">
              <div>
                <label className="block text-sm font-medium">Nome</label>
                <input type="text" className="w-full border p-2 rounded" value={name} onChange={e => setName(e.target.value)} required />
              </div>
              <div>
                <label className="block text-sm font-medium">Descrição</label>
                <textarea className="w-full border p-2 rounded" value={desc} onChange={e => setDesc(e.target.value)} />
              </div>
              <div>
                <label className="block text-sm font-medium">Preço</label>
                <input type="number" step="0.01" className="w-full border p-2 rounded" value={price} onChange={e => setPrice(e.target.value)} required />
              </div>
              <div>
                <label className="block text-sm font-medium">Imagem</label>
                <input type="file" className="w-full border p-2 rounded" onChange={e => setImageFile(e.target.files[0])} />
                {imageUrl && <p className="text-xs text-gray-500 mt-1">Imagem atual: {imageUrl}</p>}
              </div>
              <div className="flex justify-end gap-2 mt-6">
                <button type="button" onClick={closeModal} className="px-4 py-2 text-gray-600 hover:bg-gray-100 rounded">Cancelar</button>
                <button type="submit" className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Salvar</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};