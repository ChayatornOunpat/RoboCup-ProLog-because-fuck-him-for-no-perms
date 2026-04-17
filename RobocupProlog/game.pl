% ============================================================
%  game.pl
%
%  server.pl consults this file. The real engine now lives in
%  minmax.pl (2v2 grid soccer with minimax + priority agents).
%  Keeping this stub means server.pl does not need to change.
% ============================================================

:- consult('minmax.pl').
