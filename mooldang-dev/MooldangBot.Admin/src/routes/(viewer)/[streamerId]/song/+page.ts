import { redirect } from '@sveltejs/kit';

export const load = ({ params }) => {
    // [물멍]: 레거시 /song 주소로 들어오면 공식 연회장 소속 신청곡 페이지(/viewer/song)로 안내합니다.
    throw redirect(308, `/${params.streamerId}/viewer/song`);
};
