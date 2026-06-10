import { createColumnHelper } from '@tanstack/react-table'
import { useMemo, useState } from 'react'
import { useParams } from 'react-router-dom'

import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { DataTable } from '@shared/ui/data-table/DataTable.jsx'
import { mudekUsedExamTypeTr } from '../../course-evaluation/lib/mudekDisplayLabels'
import { useCourseEvaluationMudekData } from '../../course-evaluation/model/useCourseEvaluationMudekData'
import { EvaluationMudekBackToolbar } from '../../course-evaluation/ui/EvaluationMudekBackToolbar.jsx'
import styles from './MudekStudentResultsPage.module.css'

const columnHelper = createColumnHelper()

export function MudekStudentResultsPage() {
  const { offeringId } = useParams()
  const d = useCourseEvaluationMudekData(offeringId)

  const [studentFilter, setStudentFilter] = useState('')

  const best = useMemo(() => {
    const id = d.studentStats.bestId
    if (!id) return null
    return d.sortedStudents.find((x) => x.id === id) ?? null
  }, [d.sortedStudents, d.studentStats.bestId])

  const worst = useMemo(() => {
    const id = d.studentStats.worstId
    if (!id) return null
    return d.sortedStudents.find((x) => x.id === id) ?? null
  }, [d.sortedStudents, d.studentStats.worstId])

  const avg = useMemo(() => {
    const id = d.studentStats.avgId
    if (!id) return null
    return d.sortedStudents.find((x) => x.id === id) ?? null
  }, [d.sortedStudents, d.studentStats.avgId])

  const columns = useMemo(
    () => [
      columnHelper.accessor('enrollmentId', {
        header: 'Öğrenci',
        cell: (info) =>
          d.studentByEnrollmentId.get(String(info.getValue() ?? '')) ?? d.shortGuid(info.getValue()),
      }),
      columnHelper.accessor('midtermScore', { header: 'Vize' }),
      columnHelper.accessor('finalScore', { header: 'Final' }),
      columnHelper.accessor('makeupScore', { header: 'Bütleme' }),
      columnHelper.accessor('usedExamType', {
        header: 'Kullanılan sınav',
        cell: (info) => mudekUsedExamTypeTr(info.getValue()),
      }),
      columnHelper.accessor('successGrade', { header: 'Başarı (örn.)' }),
      columnHelper.accessor('letterGrade', { header: 'Harf notu' }),
      columnHelper.accessor('isPassed', { header: 'Geçti mi?', cell: (info) => (info.getValue() ? 'Evet' : 'Hayır') }),
      columnHelper.accessor('includedInStatistics', {
        header: 'İstatistikte mi?',
        cell: (info) => (info.getValue() ? 'Evet' : 'Hayır'),
      }),
      columnHelper.accessor('updatedAt', {
        header: 'Güncellendi',
        cell: (info) => d.formatDate(info.getValue()),
      }),
    ],
    [d],
  )

  const fmtPct = (v) => {
    if (v == null) return '—'
    return `%${Math.round(Number(v) * 100)}`
  }

  const fmtNum = (v) => {
    if (v == null) return '—'
    const n = Number(v)
    if (!Number.isFinite(n)) return '—'
    return n.toFixed(2)
  }

  return (
    <PageSection title={`${d.title} · MÜDEK · Öğrenci`} description="Başarı setleri + öğrenci sonuçları" error={d.error} loading={d.loading}>
      <div className={styles.root}>
        <EvaluationMudekBackToolbar />
        <div className={styles.cards}>
          <div className={styles.card}>
            <div className={styles.cardLabel}>Sınıf ortalaması</div>
            <div className={styles.cardValue}>{fmtNum(d.classAverage)}</div>
          </div>
          <div className={styles.card}>
            <div className={styles.cardLabel}>Geçme oranı</div>
            <div className={styles.cardValue}>{fmtPct(d.passRate)}</div>
          </div>
          <div className={styles.card}>
            <div className={styles.cardLabel}>İstatistikteki öğrenci</div>
            <div className={styles.cardValue}>{d.letterGradeDistribution.total}</div>
          </div>
        </div>

        <div className={styles.cards}>
          <div className={`${styles.card} ${styles.cardBest}`}>
            <div className={styles.cardLabel}>En başarılı</div>
            <div className={styles.cardValue}>
              {best ? d.studentByEnrollmentId.get(String(best.enrollmentId)) ?? d.shortGuid(best.enrollmentId) : '—'}
            </div>
            <div className={styles.cardHint}>{best ? `Başarı: ${best.successGrade ?? '—'} · ${best.letterGrade ?? '—'}` : ''}</div>
          </div>
          <div className={`${styles.card} ${styles.cardAvg}`}>
            <div className={styles.cardLabel}>Ortalama öğrenci</div>
            <div className={styles.cardValue}>
              {avg ? d.studentByEnrollmentId.get(String(avg.enrollmentId)) ?? d.shortGuid(avg.enrollmentId) : '—'}
            </div>
            <div className={styles.cardHint}>{avg ? `Başarı: ${avg.successGrade ?? '—'} · ${avg.letterGrade ?? '—'}` : ''}</div>
          </div>
          <div className={`${styles.card} ${styles.cardWorst}`}>
            <div className={styles.cardLabel}>En başarısız</div>
            <div className={styles.cardValue}>
              {worst ? d.studentByEnrollmentId.get(String(worst.enrollmentId)) ?? d.shortGuid(worst.enrollmentId) : '—'}
            </div>
            <div className={styles.cardHint}>
              {worst ? `Başarı: ${worst.successGrade ?? '—'} · ${worst.letterGrade ?? '—'}` : ''}
            </div>
          </div>
        </div>

        <div className={styles.panel}>
          <h3 className={styles.panelTitle}>Harf notu dağılımı</h3>
          {d.letterGradeDistribution.items.length ? (
            <div className={styles.dist}>
              {d.letterGradeDistribution.items.map((x) => (
                <div key={x.grade} className={styles.distRow}>
                  <div className={styles.distHead}>
                    <span className={styles.distGrade}>{x.grade}</span>
                    <span className={styles.distCount}>{x.count}</span>
                  </div>
                  <div className={styles.distBar}>
                    <div className={styles.distFill} style={{ width: `${Math.round(x.ratio * 100)}%` }} />
                  </div>
                </div>
              ))}
              <p className={sectionStyles.muted} style={{ margin: 0 }}>
                * Dağılım sadece <b>includedInStatistics</b> olan öğrencilerden hesaplanır.
              </p>
            </div>
          ) : (
            <p className={sectionStyles.muted} style={{ margin: 0 }}>
              Dağılım için yeterli veri yok.
            </p>
          )}
        </div>

        <div className={styles.panel}>
          <h3 className={styles.panelTitle}>MÜDEK · Öğrenci sonuçları tablosu</h3>
          <DataTable
            columns={columns}
            data={d.sortedStudents}
            globalFilter={studentFilter}
            onGlobalFilterChange={setStudentFilter}
            searchPlaceholder="Öğrenci / harf notu ara…"
            isLoading={false}
            disablePagination
            getRowClassName={(row) => {
              const id = row?.id
              if (!id) return ''
              if (id === d.studentStats.bestId) return 'rowBest'
              if (id === d.studentStats.worstId) return 'rowWorst'
              if (id === d.studentStats.avgId) return 'rowAvg'
              return ''
            }}
          />
        </div>
      </div>
    </PageSection>
  )
}

