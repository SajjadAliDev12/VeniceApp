import axios from 'axios';
import type { SalesSummaryDto } from '../models/reports';
import type { Product } from '../models/products';
import type { Category } from '../models/products';
import type { UserRow } from '../models/UserRow';

const API_BASE_URL = 'http://192.168.88.50:5000/api/';

const api = axios.create({
    baseURL: API_BASE_URL,
    headers: { 'Content-Type': 'application/json' },
});

// إرفاق التوكن تلقائياً مع كل طلب بعد تسجيل الدخول
api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

// خدمات الدخول والتقارير
export const login = async (username: string, password: string) => {
    const response = await api.post('auth/login', { username, password });
    return response.data; // يرجع التوكن وبيانات المستخدم
};

export const getSalesSummary = async (): Promise<SalesSummaryDto> => {
    const response = await api.get<SalesSummaryDto>('reports/sales-summary');
    return response.data;
};

// خدمات المنتجات والتصنيفات
export const getProducts = async (): Promise<Product[]> => {
    const response = await api.get<Product[]>('products');
    return response.data;
};

export const addProduct = async (product: Omit<Product, 'id' | 'categoryName'>) => {
    return await api.post('products/AddProduct', product);
};

export const updateProduct = async (id: number, product: Omit<Product, 'id' | 'categoryName'>) => {
    return await api.put(`products/${id}`, product);
};

export const deleteProduct = async (id: number) => {
    return await api.delete(`products/${id}`);
};

export const getCategories = async (): Promise<Category[]> => {
    const response = await api.get<Category[]>('Categories');
    return response.data;
};
export const getUsers = async (): Promise<UserRow[]> => {
    const response = await api.get<UserRow[]>('users');
    return response.data;
};

export const createUser = async (user: any) => {
    return await api.post('users', user);
};

export const updateUser = async (id: number, user: any) => {
    return await api.put(`users/${id}`, user);
};

export const disableUser = async (id: number) => {
    return await api.delete(`users/${id}`);
};