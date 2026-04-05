import { redirect } from '@sveltejs/kit';

export const load = ({ params }) => {
    // [물멍]: 루트 주소로 들어오면 공식 연회장(/viewer)으로 안내합니다.
    throw redirect(308, `/${params.streamerId}/viewer`);
};
