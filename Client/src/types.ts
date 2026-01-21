export interface Message {
    id: string;
    role: 'user' | 'assistant';
    content: string; // text or raw HTML
    isHtml?: boolean;
    isLoading?: boolean;
}
