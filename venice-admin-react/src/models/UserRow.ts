export interface UserRow {
    id: number;
    username: string;
    fullName: string;
    emailAddress?: string | null;
    role: number;
}