import express from 'express';
import WsProxyServer from '../grpcweb/WsProxyServer';

// import {
//   getAllNotes, getNote, addNote, editNote, removeNote,
// } from '../controllers/notes';

const router = express.Router();

// router.get('/', getAllNotes);
// router.get('/:id', getNote);
// router.post('/', addNote);
// router.put('/:id', editNote);
// router.delete('/:id', removeNote);

export const grpcwps = new WsProxyServer(8081);

export default router;
