export interface Product {
    id: number;
    name: string;
    price: number;
    isAvailable: boolean;
    categoryId: number;
    categoryName: string;
    isKitchenItem: boolean;
}

export interface Category {
    id: number;
    name: string;
}